const express = require('express');
const fs = require('fs');
const path = require('path');
const { WebcastPushConnection } = require('tiktok-live-connector');

const app = express();
const PORT = 3000;
const CONFIG_PATH = path.join(__dirname, 'config.json');

app.use(express.urlencoded({ extended: true }));
app.use(express.json());
app.use(express.static('public'));

let tiktokLiveConnection = null;

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
// 【新規追加】Web UIからのテスト実行用API
// -----------------------------------------
app.post('/api/test-event', async (req, res) => {
    const { eventType } = req.body;
    const config = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf-8'));
    
    if (!config.apiUrl || !config.apiToken || !config.steamId) {
        return res.status(400).json({ success: false, error: '環境設定が完了していません。先に保存してください。' });
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
// TikTok LIVE 連携メインロジック
// -----------------------------------------
function startTikTokIntegration() {
    const config = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf-8'));
    
    if (!config.tiktokId) {
        console.log('⚠️ TikTok IDが未設定です。Web UIから設定してください。');
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
        sendEventToCentralServer(config, 'follow_reward');
    });

    tiktokLiveConnection.on('gift', data => {
        console.log(`[LIVE] ${data.uniqueId} sent gift: ${data.giftName}`);
        if (data.giftName === 'Rose') {
            sendEventToCentralServer(config, 'reduce_food');
        } else if (data.giftName === 'Heart Me' || data.giftName === 'ハートミー') {
            sendEventToCentralServer(config, 'spawn_bear');
        }
    });
}

// -----------------------------------------
// WEB UI用の設定読み書き
// -----------------------------------------
app.get('/api/config', (req, res) => {
    res.json(JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf-8')));
});
app.post('/api/config', (req, res) => {
    fs.writeFileSync(CONFIG_PATH, JSON.stringify(req.body, null, 2));
    startTikTokIntegration(); // 保存時に再接続をトリガー
    res.redirect('/?saved=true');
});

app.listen(PORT, () => {
    console.log(`🌐 Local Web UI running at http://localhost:${PORT}`);
    startTikTokIntegration();
});
