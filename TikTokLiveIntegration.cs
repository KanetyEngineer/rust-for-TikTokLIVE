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
        
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            timer.Once(1f, () => GiveBurlapKit(player));
        }

        private void OnPlayerSpawn(BasePlayer player)
        {
            if (player == null || player.IsSleeping()) return;
            GiveBurlapKit(player);
        }

        private void GiveBurlapKit(BasePlayer player)
        {
            foreach (string itemName in burlapKit)
            {
                Item item = ItemManager.CreateByName(itemName, 1);
                if (item != null)
                {
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

            ExecuteEvent(targetPlayer, eventType);
        }

        // -------------------------------------------------------------
        // ゲーム内チャットからの動作テスト用コマンド（管理者のみ）
        // 使い方: /tiktoktest follow_reward | reduce_food | spawn_bear
        // -------------------------------------------------------------
        [ChatCommand("tiktoktest")]
        private void CmdTikTokTest(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage("<color=#ff3333>[TikTokLIVE]</color> このコマンドは管理者のみ使用できます。");
                return;
            }

            if (args.Length < 1)
            {
                player.ChatMessage("使い方: /tiktoktest <follow_reward|reduce_food|spawn_bear>");
                return;
            }

            ExecuteEvent(player, args[0]);
        }

        // -------------------------------------------------------------
        // イベント種別ごとの処理本体（RCON経由・チャットコマンド経由の共通ロジック）
        // -------------------------------------------------------------
        private void ExecuteEvent(BasePlayer targetPlayer, string eventType)
        {
            switch (eventType)
            {
                case "reduce_food": // バラ：食料ゲージを1減らす
                    float currentCalories = targetPlayer.metabolism.calories.value;
                    targetPlayer.metabolism.calories.value = Mathf.Max(0f, currentCalories - 1f);
                    targetPlayer.ChatMessage("<color=#ff3333>🌹 バラが贈られた！食料が1減少した！</color>");
                    break;

                case "spawn_bear": // ハートミー：近くにクマを出現
                    Vector3 spawnPos = targetPlayer.transform.position + (targetPlayer.transform.forward * 3f);
                    SpawnEntity("assets/rust.ai/agents/bear/bear.prefab", spawnPos);
                    targetPlayer.ChatMessage("<color=#ff33a3>❤️ ハートミー！野生のクマが野生をあらわした！</color>");
                    break;

                case "follow_reward": // フォロー：木材、スクラップ、石材からランダムで10個
                    string selectedItem = followRewards[UnityEngine.Random.Range(0, followRewards.Count)];
                    Item rewardItem = ItemManager.CreateByName(selectedItem, 10);
                    if (rewardItem != null)
                    {
                        targetPlayer.inventory.GiveItem(rewardItem);
                        targetPlayer.ChatMessage($"<color=#33ff33>✨ フォロー感謝！ {selectedItem} を10個獲得しました！</color>");
                    }
                    break;

                default:
                    targetPlayer.ChatMessage($"<color=#ff3333>[TikTokLIVE]</color> 不明なイベント種別: {eventType}");
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