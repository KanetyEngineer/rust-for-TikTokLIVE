const express = require('express');
const { Rcon } = require('rcon-client');
const app = express();

app.use(express.json());

// 安全管理のための設定（環境変数などで管理するのが理想）
const SERVER_CONFIG = {
    apiToken: "SECRET_ACCESS_TOKEN_12345", // プレイヤーと共有する認証トークン
    rconIp: "127.0.0.1",
    rconPort: 28016,
    rconPassword: "ULTRA_SECRET_RCON_PASSWORD"
};

// RCONクライアントの初期化
const rcon = new Rcon({
    host: SERVER_CONFIG.rconIp,
    port: SERVER_CONFIG.rconPort,
    password: SERVER_CONFIG.rconPassword
});

// サーバー起動時にRCON接続
rcon.connect()
    .then(() => console.log('✅ Connected directly to Rust Server via RCON'))
    .catch(err => console.error('❌ RCON Connection failed:', err));

// プレイヤー側Node.jsからリクエストを受け取るエンドポイント
app.post('/api/event', async (req, res) => {
    const { token, steamId, eventType } = req.body;

    // 1. トークン認証
    if (token !== SERVER_CONFIG.apiToken) {
        return res.status(401).json({ error: 'Unauthorized: Invalid Token' });
    }

    if (!steamId || !eventType) {
        return res.status(400).json({ error: 'Missing parameters' });
    }

    try {
        // 2. 安全にRCONコマンドをRustサーバーへ転送
        console.log(`[API] Executing event: ${eventType} for SteamID: ${steamId}`);
        const response = await rcon.send(`tiktok.event ${steamId} ${eventType}`);
        res.json({ success: true, serverResponse: response });
    } catch (error) {
        console.error('RCON send error:', error);
        res.status(500).json({ error: 'Failed to send command to Rust server' });
    }
});

const PORT = 5000; // 中継サーバーはポート5000等でリクエストを待機
app.listen(PORT, () => {
    console.log(`🚀 Central API Server running on port ${PORT}`);
});
