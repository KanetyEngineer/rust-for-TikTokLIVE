@echo off
title TikTokLIVE Rust Integration
echo ===================================
echo   TikTokLIVE Rust Integration
echo ===================================

:: 依存関係のインストール（初回のみ実行されるよう判定）
if not exist "node_modules\" (
    echo [INFO] 初回起動処理を実行しています。パッケージをインストール中...
    npm install
)

:: Web UIをデフォルトブラウザで開く
start http://localhost:3000

:: アプリケーションの起動
echo [INFO] アプリケーションを起動しています...
node server.js

pause
