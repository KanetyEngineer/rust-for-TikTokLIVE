これまでに作成したプログラムを実際の環境で動かすための、詳細なセットアップと実行手順を解説します。

システムは「**①Rustサーバー**」「**②中央APIサーバー**」「**③配信者のローカル環境**」の3つの構成要素から成り立っています。順番にセットアップを行ってください。

---

## 1. Rustサーバー側の準備 (C#プラグイン)

RustサーバーにOxide（uMod）が導入されており、RCONが有効化されていることが前提となります。

1. **プラグインの配置**
Rustサーバーのディレクトリ内にある `oxide/plugins/` フォルダを開きます。
このリポジトリの `TikTokLiveIntegration.cs` をこのフォルダ内に配置します。
2. **プラグインの読み込み**
サーバーが起動中の場合は、サーバーコンソール（またはRCON）で以下のコマンドを実行し、プラグインを読み込みます。
```text
o.reload TikTokLiveIntegration
```
エラーが出ずにロード完了のメッセージが出れば成功です。
3. **RCON設定の確認**
サーバーの起動パラメータ（`.bat` ファイルや systemd の起動コマンドなど）に、RCONポートとパスワードが設定されていることを確認してください。
*例:* `+rcon.port 28016 +rcon.password "YOUR_PASSWORD" +rcon.web 1`

---

## 2. 中央APIサーバーの構築 (Node.js)

RCONパスワードを保護し、コマンドを中継するサーバーを立ち上げます。このサーバーは、Rustサーバーと同じPC、またはポート開放済みのVPSなどで実行します。

1. **作業フォルダの準備**
このリポジトリを任意の場所に配置します。
2. **パッケージのインストール**
コマンドプロンプト（またはターミナル）でこのフォルダを開き、以下のコマンドを実行します。
```bash
npm install
```
3. **設定の書き換え**
`central-server.js` 内の `SERVER_CONFIG` の項目を、ご自身のRustサーバーのRCON情報に合わせて書き換えてください（IP、ポート、パスワード、および配信者と共有する任意の `apiToken`）。
4. **APIサーバーの起動**
以下のコマンドで中継サーバーを起動します。
```bash
node central-server.js
```
コンソールに `✅ Connected directly to Rust Server via RCON` と表示されれば待機完了です。この画面は起動したままにしてください。

---

## 3. 配信者(ローカル)側の環境構築

配信を行う各プレイヤーが、自身のPC上で設定・実行する手順です。

1. **リポジトリの配置**
このリポジトリを任意の場所に配置します（`server.js` / `start.bat` / `config.json` / `public/index.html` が含まれています）。
2. **初回起動とインストール**
`start.bat` をダブルクリックして起動します。
初回は自動的に `npm install` が実行され、必要なライブラリ（`express`, `tiktok-live-connector`）がダウンロードされます。
3. **Web UIへのアクセス**
サーバーの起動が完了すると、自動的にブラウザが立ち上がり `http://localhost:3000` の画面が表示されます。

> ⚠️ **ブラウザが `ERR_CONNECTION_REFUSED` になる場合**: Node.js v22.4.0には、パスに日本語などの非ASCII文字を含むフォルダで動かすとプロセスがクラッシュする既知の不具合があります。`nvm` などで別バージョン（v22.14.0以降推奨）に切り替え、`npm install` をやり直してください。

---

## 4. テストと実際の運用フロー

配信準備から本番までの具体的な流れです。

### 初期設定とギフトの割り当て

1. ローカルのWeb UI（`http://localhost:3000`）を開きます。
2. 「**環境設定**」カードに以下を入力し、「**設定を保存して接続**」をクリックします。
   * **TikTok ID**: ご自身のTikTokID
   * **Steam ID**: ご自身のRustでのSteamID (64bit)
   * **中継API URL**: `http://<中央APIサーバーのIP>:5000/api/event`
   * **トークン**: 中央サーバーで設定した `apiToken`
3. 「**ギフト → アクション設定**」カードで、ギフト名とアクション（下記20種類から選択）の組み合わせを設定し、「保存」をクリックします。初期状態では「バラ→食料減少」「ハートミー→クマ召喚」が登録済みです。

### 事前テスト（配信外）

TikTok LIVEを開始していなくても、次の3通りの方法でイベントの動作確認ができます。

**方法A: Web UIのテストボタン（本番と同じ経路を検証）**

1. 画面下部の「**テスト・デバッグ実行**」パネルから、試したいアクションのボタンをクリックします（20種類すべて用意されています）。
2. Rustゲーム内でアクションが反映されれば連携テストは成功です。

**方法B: ゲーム内チャットコマンド（Rustプラグイン単体の動作確認）**

Web UIや中央APIサーバーを介さず、Rustプラグイン単体の動作をすぐに確認したい場合はこちらが手軽です。管理者権限を持つプレイヤーが、ゲーム内の**チャット欄**（Enterキーで開く。F1の開発者コンソールでは反応しません）から実行します。

```text
/tiktoktest heal_player
/tiktoktest spawn_wolves
/tiktoktest teleport_random
```

（`eventType` の部分は下記アクション一覧の好きな種類に差し替え可能です）

**方法C: RCON / サーバーコンソールコマンド（管理者向け）**

任意のプレイヤーのSteamIDを指定してイベントを発火できます。RCON経由でも、サーバーコンソールからでも実行可能です。

```text
tiktok.event <SteamID> heal_player
tiktok.event <SteamID> spawn_wolves
tiktok.event <SteamID> teleport_random
```

### アクション一覧（全20種類）

**妨害系**: `reduce_food`（食料減少） / `reduce_water`（水分減少） / `damage_player`（ダメージ） / `spawn_bear`（クマ召喚） / `spawn_wolves`（オオカミ召喚） / `strip_weapon`（武器没収） / `drop_random_item`（アイテムドロップ） / `teleport_random`（ランダムテレポート） / `blind_flash`（目くらまし） / `freeze_player`（フリーズ）

**サポート系**: `follow_reward`（資材付与） / `heal_player`（HP回復） / `restore_food`（食料全回復） / `restore_water`（水分全回復） / `give_weapon`（武器支給） / `give_medkit`（医療キット支給） / `give_building_materials`（建材支給） / `gather_boost`（採集量アップ） / `comfort_boost`（快適度アップ） / `remove_bleeding`（出血解除）

### 本番の配信フロー

1. Rustサーバーにログインしておきます。
2. スマートフォンや配信ソフト（OBS等）から、**TikTok LIVEの配信を開始**します。
3. ローカル環境の `start.bat` を起動します（すでに起動している場合はそのままで問題ありません）。
4. コンソール画面に `✅ Connected to TikTok LIVE` と表示されれば、ライブ配信との同期が完了しています。
5. 視聴者がフォローや、設定したギフトを送ると、自動的にRustゲーム内にアクションが発生します。終了時は、黒いコンソール画面の「×」ボタンを押してツールを終了させてください。
