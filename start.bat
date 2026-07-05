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

:: アプリケーションの起動（Web UIはサーバーの起動が完了してから server.js 自身が開く）
echo [INFO] アプリケーションを起動しています...
node server.js

pause
