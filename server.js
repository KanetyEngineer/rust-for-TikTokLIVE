const express = require('express');
const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');
const { WebcastPushConnection } = require('tiktok-live-connector');

const app = express();
const PORT = 3000;
const CONFIG_PATH = path.join(__dirname, 'config.json');

// Rustプラグイン(TikTokLiveIntegration.cs)のExecuteEvent()が実装しているイベント種別（妨害系10種+サポート系10種）
const AVAILABLE_EVENT_TYPES = [
    // 妨害系
    'reduce_food',
    'reduce_water',
    'damage_player',
    'spawn_bear',
    'spawn_wolves',
    'strip_weapon',
    'drop_random_item',
    'teleport_random',
    'blind_flash',
    'freeze_player',
    // サポート系
    'follow_reward',
    'heal_player',
    'restore_food',
    'restore_water',
    'give_weapon',
    'give_medkit',
    'give_building_materials',
    'gather_boost',
    'comfort_boost',
    'remove_bleeding'
];

app.use(express.json());
app.use(express.static('public'));

let tiktokLiveConnection = null;

function readConfig() {
    return JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf-8'));
}

// 中央サーバーへイベントを送信する関数（テスト機能からも使えるようにResultを返すように改良）
async function sendEventToCentralServer(config, eventType) {
    try {
        const response = await fetch(config.apiUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                token: config.apiToken,
                steamId: config.steamId,
                eventType: eventType
            })
        });
        const result = await response.json();
        if (!result.success) {
            console.error('❌ Server returned error:', result.error);
        }
        return result;
    } catch (e) {
        console.error('❌ Failed to connect to Central API Server:', e.message);
        return { success: false, error: e.message };
    }
}

// -----------------------------------------
// Web UIからのテスト実行用API
// -----------------------------------------
app.post('/api/test-event', async (req, res) => {
    const { eventType } = req.body;
    const config = readConfig();

    if (!config.apiUrl || !config.apiToken || !config.steamId) {
        return res.status(400).json({ success: false, error: '環境設定が完了していません。Web UI上部の設定欄で保存してください。' });
    }

    console.log(`[DEBUG] テストイベント実行: ${eventType}`);
    const result = await sendEventToCentralServer(config, eventType);

    if (result && result.success) {
        res.json({ success: true });
    } else {
        res.status(500).json({ success: false, error: result ? result.error : 'Unknown error' });
    }
});

// -----------------------------------------
// ギフト → アクション マッピングの読み書きAPI（Web UIから設定）
// -----------------------------------------
app.get('/api/gift-mappings', (req, res) => {
    const config = readConfig();
    res.json({
        giftMappings: config.giftMappings || [],
        availableEventTypes: AVAILABLE_EVENT_TYPES
    });
});

app.post('/api/gift-mappings', (req, res) => {
    const { giftMappings } = req.body;
    if (!Array.isArray(giftMappings)) {
        return res.status(400).json({ success: false, error: 'giftMappings must be an array' });
    }

    const sanitized = giftMappings
        .map(m => ({
            giftName: String(m.giftName || '').trim(),
            eventType: String(m.eventType || '').trim()
        }))
        .filter(m => m.giftName && AVAILABLE_EVENT_TYPES.includes(m.eventType));

    const config = readConfig();
    config.giftMappings = sanitized;
    fs.writeFileSync(CONFIG_PATH, JSON.stringify(config, null, 2));

    console.log(`[DEBUG] ギフトマッピングを更新しました (${sanitized.length}件)`);
    res.json({ success: true, giftMappings: sanitized });
});

// -----------------------------------------
// TikTok LIVE 連携メインロジック
// -----------------------------------------
function startTikTokIntegration() {
    const config = readConfig();

    if (!config.tiktokId) {
        console.log('⚠️ TikTok IDが未設定です。Web UI（http://localhost:3000）から設定してください。');
        return;
    }

    if (tiktokLiveConnection) {
        tiktokLiveConnection.disconnect();
    }

    tiktokLiveConnection = new WebcastPushConnection(config.tiktokId);
    console.log(`⏳ ${config.tiktokId} のライブ配信を検索中...`);

    tiktokLiveConnection.connect().then(state => {
        console.info(`✅ Connected to TikTok LIVE: ${state.roomId}`);
    }).catch(err => {
        console.error('❌ 接続待機中: 配信が開始されていないか、IDが間違っています。');
        console.log('🔄 10秒後に再試行します...');
        setTimeout(startTikTokIntegration, 10000);
        return;
    });

    tiktokLiveConnection.on('follow', data => {
        console.log(`[LIVE] ${data.uniqueId} followed you!`);
        sendEventToCentralServer(readConfig(), 'follow_reward');
    });

    // ギフト名とアクションの対応は config.json の giftMappings（Web UIで編集可能）から都度読み込む。
    // こうすることで、Web UIでマッピングを変更してもツールを再起動せずに反映される。
    tiktokLiveConnection.on('gift', data => {
        console.log(`[LIVE] ${data.uniqueId} sent gift: ${data.giftName}`);
        const currentConfig = readConfig();
        const mapping = (currentConfig.giftMappings || []).find(m => m.giftName === data.giftName);
        if (mapping) {
            sendEventToCentralServer(currentConfig, mapping.eventType);
        } else {
            console.log(`[DEBUG] ギフト「${data.giftName}」に対応するアクションは設定されていません。`);
        }
    });
}

// -----------------------------------------
// WEB UI用の設定読み書き（tiktokId / steamId / apiUrl / apiToken）
// -----------------------------------------
app.get('/api/config', (req, res) => {
    res.json(readConfig());
});

app.post('/api/config', (req, res) => {
    const { tiktokId, steamId, apiUrl, apiToken } = req.body;

    const config = readConfig();
    if (typeof tiktokId === 'string') config.tiktokId = tiktokId.trim();
    if (typeof steamId === 'string') config.steamId = steamId.trim();
    if (typeof apiUrl === 'string') config.apiUrl = apiUrl.trim();
    if (typeof apiToken === 'string') config.apiToken = apiToken.trim();

    fs.writeFileSync(CONFIG_PATH, JSON.stringify(config, null, 2));
    console.log('[DEBUG] 環境設定を更新しました。TikTok LIVE接続を再開します。');

    startTikTokIntegration(); // tiktokIdが変わった場合に備えて再接続
    res.json({ success: true, config });
});

// OSのデフォルトブラウザでURLを開く（Windows/macOS/Linux対応）
function openBrowser(url) {
    const command = process.platform === 'win32'
        ? `start "" "${url}"`
        : process.platform === 'darwin'
            ? `open "${url}"`
            : `xdg-open "${url}"`;

    exec(command, (err) => {
        if (err) {
            console.error('⚠️ ブラウザの自動起動に失敗しました。手動でアクセスしてください:', url);
        }
    });
}

app.listen(PORT, () => {
    console.log(`🌐 Local Web UI running at http://localhost:${PORT}`);
    // サーバーが実際にリッスンを開始した後にブラウザを開くことで、
    // ERR_CONNECTION_REFUSED（ブラウザの方が先に開いてしまう競合状態）を防ぐ。
    openBrowser(`http://localhost:${PORT}`);
    startTikTokIntegration();
});
