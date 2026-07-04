using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("TikTokLiveIntegration", "ProjectTeam", "1.1.0")]
    [Description("Handles TikTok LIVE integration events securely and basic setup kits.")]
    public class TikTokLiveIntegration : RustPlugin
    {
        // 麻の服一式のアイテムShortnameリスト
        private readonly List<string> burlapKit = new List<string>
        {
            "attire.burlap.shirt",
            "attire.burlap.trousers",
            "shoes.burlap.shoes"
        };

        // ランダムで付与するアイテムのリスト（フォロー用）
        private readonly List<string> followRewards = new List<string> { "wood", "scrap", "stones" };

        // -------------------------------------------------------------
        // フック：プレイヤーログイン時 ＆ リスポーン時
        // -------------------------------------------------------------

        // ログイン（接続完了）時
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            // ログイン直後はインベントリの準備ができていない場合があるため1秒遅らせる
            timer.Once(1f, () => GiveBurlapKit(player));
        }

        // リスポーン時
        private void OnPlayerSpawn(BasePlayer player)
        {
            if (player == null || player.IsSleeping()) return;
            GiveBurlapKit(player);
        }

        // 服を無条件で与えるヘルパー関数
        private void GiveBurlapKit(BasePlayer player)
        {
            foreach (string itemName in burlapKit)
            {
                Item item = ItemManager.CreateByName(itemName, 1);
                if (item != null)
                {
                    // 衣服スロット、またはメインインベントリへ
                    if (!item.MoveToContainer(player.inventory.containerWear))
                    {
                        item.MoveToContainer(player.inventory.containerMain);
                    }
                }
            }
            player.ChatMessage("<color=#ffaa00>[TikTokLIVE]</color> 麻の服一式が支給されました。");
        }

        // -------------------------------------------------------------
        // RCONコマンドからのイベント実行分岐
        // -------------------------------------------------------------
        [ConsoleCommand("tiktok.event")]
        private void CmdTikTokEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2) return;

            ulong steamId;
            if (!ulong.TryParse(arg.Args[0].ToString(), out steamId)) return;
            
            string eventType = arg.Args[1].ToString();
            BasePlayer targetPlayer = BasePlayer.FindByID(steamId);

            if (targetPlayer == null || !targetPlayer.IsConnected) return;

            switch (eventType)
            {
                case "reduce_food": // バラ：食料ゲージを1減らす
                    // Rustの代謝システム(metabolism)のcalories(食料)を変更
                    float currentCalories = targetPlayer.metabolism.calories.value;
                    targetPlayer.metabolism.calories.value = Mathf.Max(0f, currentCalories - 1f);
                    targetPlayer.metabolism.SendChangesToClient(); // クライアントへ同期
                    targetPlayer.ChatMessage("<color=#ff3333>🌹 バラが贈られた！食料が1減少した！</color>");
                    break;

                case "spawn_bear": // ハートミー：近くにクマを出現
                    Vector3 spawnPos = targetPlayer.transform.position + (targetPlayer.transform.forward * 3f); // プレイヤーの3m前方
                    SpawnEntity("assets/rust.ai/agents/bear/bear.prefab", spawnPos);
                    targetPlayer.ChatMessage("<color=#ff33a3>❤️ ハートミー！野生のクマが野生をあらわした！</color>");
                    break;

                case "follow_reward": // フォロー：木材、スクラップ、石材からランダムで10個
                    string selectedItem = followRewards[Random.Range(0, followRewards.Count)];
                    Item rewardItem = ItemManager.CreateByName(selectedItem, 10);
                    if (rewardItem != null)
                    {
                        player.inventory.GiveItem(rewardItem);
                        targetPlayer.ChatMessage($"<color=#33ff33>✨ フォロー感謝！ {selectedItem} を10個獲得しました！</color>");
                    }
                    break;

                default:
                    break;
            }
        }

        private void SpawnEntity(string prefabPath, Vector3 position)
        {
            BaseEntity entity = GameManager.server.CreateEntity(prefabPath, position, new Quaternion(), true);
            if (entity != null)
            {
                entity.Spawn();
            }
        }
    }
}
