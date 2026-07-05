using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("TikTokLiveIntegration", "ProjectTeam", "1.2.0")]
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

        // 建材まとめ支給用のアイテムと個数
        private readonly Dictionary<string, int> buildingMaterials = new Dictionary<string, int>
        {
            { "wood", 1000 },
            { "stones", 500 },
            { "metal.fragments", 200 }
        };

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
        // 使い方: /tiktoktest <eventType>
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
                player.ChatMessage("使い方: /tiktoktest <eventType>");
                return;
            }

            ExecuteEvent(player, args[0]);
        }

        // -------------------------------------------------------------
        // イベント種別ごとの処理本体（RCON経由・チャットコマンド経由の共通ロジック）
        // 妨害系10種 + サポート系10種の合計20種類
        // -------------------------------------------------------------
        private void ExecuteEvent(BasePlayer targetPlayer, string eventType)
        {
            switch (eventType)
            {
                // ===================== 妨害系（10種） =====================

                case "reduce_food": // 食料ゲージを減少
                    targetPlayer.metabolism.calories.value = Mathf.Max(0f, targetPlayer.metabolism.calories.value - 50f);
                    targetPlayer.ChatMessage("<color=#ff3333>🌹 バラが贈られた！食料が減少した！</color>");
                    break;

                case "reduce_water": // 水分ゲージを減少
                    targetPlayer.metabolism.hydration.value = Mathf.Max(0f, targetPlayer.metabolism.hydration.value - 50f);
                    targetPlayer.ChatMessage("<color=#3399ff>💧 水分が奪われた！</color>");
                    break;

                case "damage_player": // HPを一定量減少
                    targetPlayer.Hurt(15f);
                    targetPlayer.ChatMessage("<color=#ff3333>💥 攻撃を受けた！HPが減少した！</color>");
                    break;

                case "spawn_bear": // 近くにクマを出現
                    SpawnEntity("assets/rust.ai/agents/bear/bear.prefab", targetPlayer.transform.position + (targetPlayer.transform.forward * 3f));
                    targetPlayer.ChatMessage("<color=#ff33a3>❤️ ハートミー！野生のクマが姿を現した！</color>");
                    break;

                case "spawn_wolves": // 近くにオオカミの群れを出現
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 offset = new Vector3(UnityEngine.Random.Range(-4f, 4f), 0f, UnityEngine.Random.Range(-4f, 4f));
                        SpawnEntity("assets/rust.ai/agents/wolf/wolf.prefab", targetPlayer.transform.position + offset);
                    }
                    targetPlayer.ChatMessage("<color=#ff33a3>🐺 オオカミの群れに囲まれた！</color>");
                    break;

                case "strip_weapon": // 手に持っている武器を没収
                    Item activeItem = targetPlayer.GetActiveItem();
                    if (activeItem != null)
                    {
                        activeItem.RemoveFromContainer();
                        activeItem.Remove();
                        targetPlayer.ChatMessage("<color=#ff3333>🗡️ 武器が奪われた！</color>");
                    }
                    else
                    {
                        targetPlayer.ChatMessage("<color=#ff3333>🗡️ 武器を持っていなかったため何も起きなかった。</color>");
                    }
                    break;

                case "drop_random_item": // インベントリからランダムに1個、足元に落とす
                    List<Item> allItems = new List<Item>();
                    allItems.AddRange(targetPlayer.inventory.containerMain.itemList);
                    allItems.AddRange(targetPlayer.inventory.containerBelt.itemList);
                    if (allItems.Count > 0)
                    {
                        Item dropItem = allItems[UnityEngine.Random.Range(0, allItems.Count)];
                        string droppedName = dropItem.info.displayName.english;
                        dropItem.Drop(targetPlayer.transform.position + Vector3.up, Vector3.zero);
                        targetPlayer.ChatMessage($"<color=#ff3333>🎒 {droppedName} を落としてしまった！</color>");
                    }
                    else
                    {
                        targetPlayer.ChatMessage("<color=#ff3333>🎒 インベントリが空のため何も起きなかった。</color>");
                    }
                    break;

                case "teleport_random": // 近隣のランダムな地点にテレポート
                    Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-50f, 50f), 0f, UnityEngine.Random.Range(-50f, 50f));
                    Vector3 newPos = targetPlayer.transform.position + randomOffset;
                    newPos.y = TerrainMeta.HeightMap.GetHeight(newPos);
                    targetPlayer.Teleport(newPos);
                    targetPlayer.ChatMessage("<color=#ff3333>🌀 突然どこかへ飛ばされた！</color>");
                    break;

                case "blind_flash": // 画面を一瞬フラッシュさせる
                    Effect.server.Run("assets/prefabs/weapons/flashbang/effects/flashbang_explosion.prefab", targetPlayer.transform.position);
                    targetPlayer.ChatMessage("<color=#ff3333>✨ 目がくらんだ！</color>");
                    break;

                case "freeze_player": // 数秒間、同じ位置にテレポートし続けて移動を封じる
                    Vector3 freezePos = targetPlayer.transform.position;
                    targetPlayer.ChatMessage("<color=#3399ff>🧊 体が凍りついて動けない！</color>");
                    timer.Repeat(0.2f, 25, () =>
                    {
                        if (targetPlayer != null && targetPlayer.IsConnected)
                        {
                            targetPlayer.Teleport(freezePos);
                        }
                    });
                    timer.Once(5f, () =>
                    {
                        if (targetPlayer != null && targetPlayer.IsConnected)
                        {
                            targetPlayer.ChatMessage("<color=#3399ff>🧊 体の自由が戻った。</color>");
                        }
                    });
                    break;

                // ===================== サポート系（10種） =====================

                case "follow_reward": // 木材、スクラップ、石材からランダムで10個
                    string selectedItem = followRewards[UnityEngine.Random.Range(0, followRewards.Count)];
                    Item rewardItem = ItemManager.CreateByName(selectedItem, 10);
                    if (rewardItem != null)
                    {
                        targetPlayer.inventory.GiveItem(rewardItem);
                        targetPlayer.ChatMessage($"<color=#33ff33>✨ フォロー感謝！ {selectedItem} を10個獲得しました！</color>");
                    }
                    break;

                case "heal_player": // HPを回復
                    targetPlayer.Heal(30f);
                    targetPlayer.ChatMessage("<color=#33ff33>💚 HPが回復した！</color>");
                    break;

                case "restore_food": // 食料ゲージを全回復
                    targetPlayer.metabolism.calories.value = 500f;
                    targetPlayer.ChatMessage("<color=#33ff33>🍖 お腹いっぱいになった！</color>");
                    break;

                case "restore_water": // 水分ゲージを全回復
                    targetPlayer.metabolism.hydration.value = 250f;
                    targetPlayer.ChatMessage("<color=#33ff33>🥤 喉の渇きが癒された！</color>");
                    break;

                case "give_weapon": // 武器(ピストル)を1つ支給
                    Item weaponItem = ItemManager.CreateByName("pistol.eoka", 1);
                    if (weaponItem != null)
                    {
                        targetPlayer.inventory.GiveItem(weaponItem);
                        targetPlayer.ChatMessage("<color=#33ff33>🔫 武器が支給された！</color>");
                    }
                    break;

                case "give_medkit": // 医療キットを支給
                    Item medItem = ItemManager.CreateByName("syringe.medical", 3);
                    if (medItem != null)
                    {
                        targetPlayer.inventory.GiveItem(medItem);
                        targetPlayer.ChatMessage("<color=#33ff33>💉 医療キットが支給された！</color>");
                    }
                    break;

                case "give_building_materials": // 建材をまとめて支給
                    foreach (KeyValuePair<string, int> mat in buildingMaterials)
                    {
                        Item matItem = ItemManager.CreateByName(mat.Key, mat.Value);
                        if (matItem != null)
                        {
                            targetPlayer.inventory.GiveItem(matItem);
                        }
                    }
                    targetPlayer.ChatMessage("<color=#33ff33>🏗️ 建材一式が支給された！</color>");
                    break;

                case "gather_boost": // 採集効率が上がるチェーンソーと燃料を支給
                    Item chainsaw = ItemManager.CreateByName("chainsaw", 1);
                    if (chainsaw != null)
                    {
                        targetPlayer.inventory.GiveItem(chainsaw);
                    }
                    Item fuel = ItemManager.CreateByName("lowgradefuel", 50);
                    if (fuel != null)
                    {
                        targetPlayer.inventory.GiveItem(fuel);
                    }
                    targetPlayer.ChatMessage("<color=#33ff33>🌾 採集が捗るチェーンソーが支給された！</color>");
                    break;

                case "comfort_boost": // 快適度を上昇
                    targetPlayer.metabolism.comfort.value = 1f;
                    targetPlayer.ChatMessage("<color=#33ff33>🛋️ 心地よい気分になった！</color>");
                    break;

                case "remove_bleeding": // 出血状態を解除
                    targetPlayer.metabolism.bleeding.value = 0f;
                    targetPlayer.ChatMessage("<color=#33ff33>🩹 出血が止まった！</color>");
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
