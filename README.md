# Rust for TikTokLIVE

TikTok LIVEの配信中のイベント（ギフト、フォローなど）をフックし、Rustのゲームサーバー内にリアルタイムで干渉（アイテム付与や妨害イベント）を行うための統合ツールです。

ストリーマー（配信者）が安全に利用できるよう、RCONパスワードを隠蔽する「中央APIサーバー」を経由する3層構造を採用しています。

## 🌟 主な機能

* **リアルタイムイベント連携**: TikTok LIVEのフォローや特定ギフトに反応し、Rust内でイベントを自動実行。
* **セキュアなアーキテクチャ**: 中央APIサーバーがRCON通信を中継することで、配信者にRCONパスワードを露出させません。
* **ローカルWeb UI**: 配信者が自身のTikTok IDやSteam IDをブラウザから簡単に設定できるGUIを搭載。
* **複数のデバッグ手段**: Web UIのテストボタン、ゲーム内チャットコマンド、RCONコマンドの3通りで、配信外でもイベントを検証可能。
* **自動再接続**: 配信開始前にツールを立ち上げても、配信開始を自動検知してシームレスに接続。

---

## 🛠️ システム要件

* **Rust Server**: Oxide (uMod) がインストールされ、RCONが有効化されていること。
* **Node.js**: v14以上推奨（中央APIサーバーおよびローカルクライアントの実行に必要）。

---

## 📦 システム構成とファイル

本システムは以下のファイル群で構成されています（すべてリポジトリのルート直下に配置します）。

```text
.
├── TikTokLiveIntegration.cs   # Rustサーバー用プラグイン (C#) → oxide/plugins/ に配置
├── central-server.js          # RCON中継用APIサーバー (Node.js)
├── server.js                  # 配信者が実行するローカルクライアント本体 (Node.js)
├── start.bat                  # ローカルクライアントのワンクリック起動バッチ
├── config.json                # ローカルクライアントの環境設定ファイル
├── package.json
└── public/
    └── index.html              # 設定・デバッグ用ローカルWeb UI
```

---

## 🚀 セットアップ手順

### 1. Rustサーバーの準備

1. `TikTokLiveIntegration.cs` をRustサーバーの `oxide/plugins/` ディレクトリに配置します。
2. サーバーコンソール（またはRCON）で `o.reload TikTokLiveIntegration` を実行し、プラグインを読み込みます。
3. サーバーの起動パラメータでRCONポートとパスワードが設定されていることを確認してください。
   *例:* `+rcon.port 28016 +rcon.password "YOUR_PASSWORD" +rcon.web 1`

### 2. 中央APIサーバーの構築 (管理者用)

RCONパスワードを保持し、ローカルクライアントからのリクエストを中継します。Rustサーバーと同じマシン、またはポート開放済みのVPSなどで実行してください。

1. リポジトリを配置したフォルダでコマンドプロンプトを開き、依存パッケージをインストールします。
   ```bash
   npm install
   ```
2. `central-server.js` 内の `SERVER_CONFIG` を、実際のRustサーバーのRCON情報および任意の `apiToken` に書き換えます。
3. サーバーを起動します。
   ```bash
   node central-server.js
   ```
   コンソールに `✅ Connected directly to Rust Server via RCON` と表示されれば待機完了です。この画面は起動したままにしてください。

### 3. ローカルクライアントの準備 (配信者用)

1. リポジトリを配信者のPCに配置します。
2. `start.bat` を実行します（初回起動時は自動で `npm install` が実行されます）。
3. 自動的にブラウザが開き、ローカルWeb UI（`http://localhost:3000`）が表示されます。

---

## 🎮 使い方と運用フロー

### 初期設定

1. Web UIにアクセスし、以下の情報を入力して「**設定を保存して接続**」をクリックします。
   * **TikTok ユーザーID**: 配信を行うTikTokのID (`@`以降)
   * **Steam ID**: イベントの対象となるプレイヤーの64bit Steam ID
   * **中継API URL**: `http://<中央APIサーバーのIP>:5000/api/event`
   * **セキュリティ認証トークン**: 管理者から共有された `apiToken`

### デバッグ・動作テスト（配信外でも可能）

TikTok LIVEを開始しなくても、以下の3通りの方法でイベントの動作確認ができます。

1. **Web UIのテストボタン**（本番と同じ経路を検証）
   Web UI下部の「**テスト・デバッグ実行**」パネルから任意のボタンをクリックすると、`ローカルクライアント → 中央APIサーバー → RCON → Rustプラグイン` という本番と同じ経路でイベントが発火します。
2. **ゲーム内チャットコマンド**（Rustプラグイン単体の動作確認）
   ゲーム内で管理者権限を持つプレイヤーが、チャット欄（Enterキーで開く）から実行できます。F1の開発者コンソールでは反応しないので注意してください。
   ```text
   /tiktoktest follow_reward
   /tiktoktest reduce_food
   /tiktoktest spawn_bear
   ```
3. **RCON / サーバーコンソールコマンド**（管理者向け、SteamID指定）
   ```text
   tiktok.event <SteamID> follow_reward
   tiktok.event <SteamID> reduce_food
   tiktok.event <SteamID> spawn_bear
   ```

### 本番の配信

1. Rustサーバーにログインします。
2. TikTok LIVEの配信を開始します。
3. `start.bat` を起動します。コンソールに `✅ Connected to TikTok LIVE` と表示されれば同期完了です。

---

## 🎁 実装済みのイベント一覧

現在、以下のイベントがRustプラグイン側に実装されています。

| トリガー (TikTok LIVE) | アクション (Rustゲーム内) | 詳細 |
| --- | --- | --- |
| **ログイン / リスポーン** | 初期装備付与 | 麻の服一式（シャツ、ズボン、靴）を無条件で自動付与 |
| **フォロー** | 資材付与 | 木材、スクラップ、石材の中からランダムで10個付与 |
| **ギフト: バラ (Rose)** | 妨害: 食料減少 | プレイヤーの食料ゲージ（カロリー）を1減少させる |
| **ギフト: ハートミー** | 妨害: クマ召喚 | プレイヤーの目の前（3m前方）に野生のクマをスポーンさせる |

※ イベントを追加・変更する場合は、`TikTokLiveIntegration.cs` の `ExecuteEvent()` 内の処理、およびローカルクライアントの判定ロジックを修正してください。`ExecuteEvent()` はチャットコマンド・RCONコマンドの両方から共通で呼ばれます。
