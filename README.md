# Rust for TikTokLIVE

TikTok LIVEの配信中のイベント（ギフト、フォローなど）をフックし、Rustのゲームサーバー内にリアルタイムで干渉（アイテム付与や妨害イベント）を行うための統合ツールです。

ストリーマー（配信者）が安全に利用できるよう、RCONパスワードを隠蔽する「中央APIサーバー」を経由する3層構造を採用しています。

## 🌟 主な機能

* **リアルタイムイベント連携**: TikTok LIVEのフォローや特定ギフトに反応し、Rust内でイベントを自動実行。
* **任意のギフト → 任意のアクション**: どのギフト名にどのアクション（妨害・サポート、全20種類）を割り当てるかを、ローカルWeb UIから自由に設定可能。
* **セキュアなアーキテクチャ**: 中央APIサーバーがRCON通信を中継することで、配信者にRCONパスワードを露出させません。
* **ローカルWeb UI**: `tiktokId` / `steamId` / 中継API URL / トークンなどの環境設定、ギフト→アクションのマッピング設定、デバッグ用のテストボタンをすべてブラウザから操作可能。
* **複数のデバッグ手段**: Web UIのテストボタン、ゲーム内チャットコマンド、RCONコマンドの3通りで、配信外でもイベントを検証可能。
* **自動再接続**: 配信開始前にツールを立ち上げても、配信開始を自動検知してシームレスに接続。

---

## 🛠️ システム要件

* **Rust Server**: Oxide (uMod) がインストールされ、RCONが有効化されていること。
* **Node.js**: v18以上推奨（中央APIサーバーおよびローカルクライアントの実行に必要）。
  > ⚠️ **既知の問題**: Node.js v22.4.0 には、パスに日本語などの非ASCII文字が含まれるフォルダで `require()` を呼ぶとプロセスがクラッシュする不具合があります（`node_modules` の内容に関わらず発生）。`start.bat` を実行してもコンソールが即座に落ちる／ブラウザで `ERR_CONNECTION_REFUSED` になる場合は、この不具合が原因の可能性が高いです。`nvm` などで別バージョン（例: v22.14.0以降）に切り替えてください。

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
    └── index.html              # 環境設定・ギフトマッピング・デバッグ用ローカルWeb UI
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
3. サーバーの起動が完了すると、自動的にブラウザが開きローカルWeb UI（`http://localhost:3000`）が表示されます。

---

## 🎮 使い方と運用フロー

### 初期設定

ローカルWeb UI（`http://localhost:3000`）の「**環境設定**」カードに、以下の項目を入力して「**設定を保存して接続**」をクリックします。

* **TikTok ユーザーID**: 配信を行うTikTokのID (`@`以降)
* **Steam ID**: イベントの対象となるプレイヤーの64bit Steam ID
* **中継API URL**: `http://<中央APIサーバーのIP>:5000/api/event`
* **セキュリティ認証トークン**: 管理者から共有された `apiToken`

保存するとその場で `config.json` に書き込まれ、ツールを再起動しなくてもTikTok LIVEへの接続が再開されます。

### ギフト → アクションの割り当て

どのギフトでどのアクションを起こすかは、同じWeb UIの「**ギフト → アクション設定**」パネルから設定します。ギフト名（例: `Rose`）と、割り当てたいアクション（下記20種類から選択）をプルダウンで選び、「保存」を押すだけです。ここでの変更は `config.json` の `giftMappings` に保存され、ツールを再起動しなくても次のギフトから反映されます。行を追加すれば、いくつでもギフトとアクションの組み合わせを登録できます。

### デバッグ・動作テスト（配信外でも可能）

TikTok LIVEを開始しなくても、以下の3通りの方法でイベントの動作確認ができます。

1. **Web UIのテストボタン**（本番と同じ経路を検証）
   Web UI下部の「**テスト・デバッグ実行**」パネルから任意のボタンをクリックすると、`ローカルクライアント → 中央APIサーバー → RCON → Rustプラグイン` という本番と同じ経路でイベントが発火します（20種類すべてのボタンがあります）。
2. **ゲーム内チャットコマンド**（Rustプラグイン単体の動作確認）
   ゲーム内で管理者権限を持つプレイヤーが、チャット欄（Enterキーで開く）から実行できます。F1の開発者コンソールでは反応しないので注意してください。
   ```text
   /tiktoktest heal_player
   /tiktoktest spawn_wolves
   /tiktoktest teleport_random
   ```
   （`eventType` の部分は下記アクション一覧の好きな種類に差し替え可能）
3. **RCON / サーバーコンソールコマンド**（管理者向け、SteamID指定）
   ```text
   tiktok.event <SteamID> heal_player
   tiktok.event <SteamID> spawn_wolves
   tiktok.event <SteamID> teleport_random
   ```

### 本番の配信

1. Rustサーバーにログインします。
2. TikTok LIVEの配信を開始します。
3. `start.bat` を起動します。コンソールに `✅ Connected to TikTok LIVE` と表示されれば同期完了です。

---

## 🎁 実装済みのアクション一覧（全20種類）

Rustプラグイン側（`TikTokLiveIntegration.cs` の `ExecuteEvent()`）には、妨害系10種・サポート系10種、計20種類のアクションが実装されています。どのギフトにどのアクションを割り当てるかは、ローカルWeb UIの「ギフト → アクション設定」パネルで自由に組み合わせられます。

### 🩹 妨害系

| アクション種別 (eventType) | 内容 |
| --- | --- |
| `reduce_food` | 食料ゲージ（カロリー）を減少させる |
| `reduce_water` | 水分ゲージ（ハイドレーション）を減少させる |
| `damage_player` | HPに一定ダメージを与える |
| `spawn_bear` | プレイヤーの目の前にクマをスポーンさせる |
| `spawn_wolves` | プレイヤーの周囲にオオカミの群れをスポーンさせる |
| `strip_weapon` | 手に持っている武器を没収する |
| `drop_random_item` | インベントリからランダムに1個、足元に落とす |
| `teleport_random` | 近隣のランダムな地点にテレポートさせる |
| `blind_flash` | 画面を一瞬フラッシュさせる |
| `freeze_player` | 数秒間、移動を封じる |

### 💚 サポート系

| アクション種別 (eventType) | 内容 |
| --- | --- |
| `follow_reward` | 木材、スクラップ、石材の中からランダムで10個付与 |
| `heal_player` | HPを回復させる |
| `restore_food` | 食料ゲージを全回復させる |
| `restore_water` | 水分ゲージを全回復させる |
| `give_weapon` | 武器（ピストル）を1つ支給する |
| `give_medkit` | 医療キットを支給する |
| `give_building_materials` | 建材（木材・石材・金属）をまとめて支給する |
| `gather_boost` | 採集効率が上がるチェーンソーと燃料を支給する |
| `comfort_boost` | 快適度（comfort）を上昇させる |
| `remove_bleeding` | 出血状態を解除する |

これとは別に、**ログイン / リスポーン時**には麻の服一式（シャツ、ズボン、靴）が無条件で自動支給されます（`OnPlayerConnected` / `OnPlayerSpawn` フック）。

初期状態では、ギフト「バラ (Rose)」→ `reduce_food`、「ハートミー (Heart Me / ハートミー)」→ `spawn_bear`、フォロー → `follow_reward` がデフォルトで登録されています。

※ 新しいアクション種別自体を追加する場合は、`TikTokLiveIntegration.cs` の `ExecuteEvent()` にケースを追加し、`server.js` の `AVAILABLE_EVENT_TYPES` と `public/index.html` の `EVENT_LABELS` にも同じ文字列を追加してください。

---

## 🩹 トラブルシューティング

### `start.bat` を実行してもブラウザが `ERR_CONNECTION_REFUSED` になる

多くの場合、Node.jsのバージョン起因でサーバープロセス自体が起動していません。上記の「システム要件」にある **Node.js v22.4.0の既知の不具合** に該当していないか確認し、該当する場合は別バージョンのNode.jsに切り替えてから `npm install` をやり直してください。
