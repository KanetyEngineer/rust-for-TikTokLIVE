using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TikTokLiveIntegration", "YourName", "1.0.0")]
    [Description("Executes events based on TikTok Live triggers via RCON")]
    public class TikTokLiveIntegration : RustPlugin
    {
        // RCON等から呼び出せるカスタムコンソールコマンド
        // 例: tiktok.event <SteamID> <EventType>
        [ConsoleCommand("tiktok.event")]
        private void CmdTikTokEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2)
            {
                Puts("Usage: tiktok.event <SteamID> <EventType>");
                return;
            }

            ulong steamId;
            if (!ulong.TryParse(arg.Args[0], out steamId)) return;

            string eventType = arg.Args[1];
            BasePlayer targetPlayer = BasePlayer.FindByID(steamId);

            if (targetPlayer == null || !targetPlayer.IsConnected)
            {
                Puts($"Player {steamId} not found or offline.");
                return;
            }

            // イベントタイプに応じた処理の分岐
            switch (eventType)
            {
                case "add_wood":
                    targetPlayer.inventory.GiveItem(ItemManager.CreateByName("wood", 1000));
                    targetPlayer.ChatMessage("TikTokから木材1000が届きました！");
                    break;

                case "spawn_bear":
                    SpawnEntity("assets/rust.ai/agents/bear/bear.prefab", targetPlayer.transform.position + new Vector3(2f, 0, 2f));
                    targetPlayer.ChatMessage("TikTokギフトによりクマが召喚されました！");
                    break;

                default:
                    Puts($"Unknown event type: {eventType}");
                    break;
            }
        }

        // エンティティをスポーンさせるヘルパーメソッド
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
