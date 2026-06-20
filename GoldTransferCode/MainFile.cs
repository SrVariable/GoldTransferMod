using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Runs;

namespace GoldTransfer.GoldTransferCode;

//You're recommended but not required to keep all your code in this package and all your assets in the GoldTransfer folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "GoldTransfer"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, LogType.Generic);
    
    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(ModId);

        harmony.PatchAll();
        Logger.Info("Hello world from Initialize");
        RunManager.Instance.RunStarted += DebugPlayers;
    }

    private static void DebugPlayers(RunState runState)
    {
        Logger.Info("Hello world from DebugPlayers");
        foreach (Player player in runState.Players)
        {
            Logger.Info($"{player.NetId}: {player.Gold}");
        }
        Logger.Info($"players:  {runState.Players.Count}");
    }

    [HarmonyPatch(typeof(NTopBarGold))]
    public class NTopBarGoldPatch
    {
        private static MessageHandlerDelegate<TransferMessage>? _activeHandler;
        private static IPlayerCollection? _playerCollection;
        
        [HarmonyPatch(nameof(NTopBarGold.Initialize))]
        [HarmonyPostfix]
        public static void Initialize(NTopBarGold __instance, Player player)
        {
            Logger.Info("Hello world from NTopBarGoldPatch.Initialize");
            _playerCollection = player.RunState;
            
            AddSendButton(__instance, player);
            RegisterMessageHandler();
        }
        
        [HarmonyPatch(nameof(NTopBarGold._ExitTree))]
        [HarmonyPostfix]
        public static void _ExitTree()
        {
            Logger.Info("Hello world from NTopBarGoldPatch._ExitTree");
            UnregisterMessageHandler();
            _playerCollection = null;
        }

        private static void AddSendButton(NTopBarGold nTopBarGold, Player player)
        {
            // TODO(srvariable): Create localization
            Button btn = new Button
            {
                Text = "Send",
                TooltipText = "This is a cool message",
            };

            Player? toPlayer = player.RunState.Players.Count > 1 ? player.RunState.Players[1] : null;
            if (toPlayer != null && toPlayer == player)
            {
                toPlayer = player.RunState.Players[0];
            }

            btn.Pressed += () => Transfer(player, toPlayer, 10);

            nTopBarGold.AddChild(btn);
        }

        private static void Transfer(Player player, Player? toPlayer, int amount)
        {
            if (toPlayer == null)
            {
                return;
            }

            if (amount <= 0 || player.Gold < amount)
            {
                return;
            }

            player.Gold -= amount;
            toPlayer.Gold += amount;
            Logger.Info($"{player.NetId}: {player.Gold}");
            Logger.Info($"{toPlayer.NetId}: {toPlayer.Gold}");

            RunManager.Instance.NetService.SendMessage(new TransferMessage
            {
                fromNetId = player.NetId,
                toNetId = toPlayer.NetId,
                amount = amount
            });
    }

        private static void RegisterMessageHandler()
        {
            _activeHandler = HandleTransferMessage;
            RunManager.Instance.NetService.RegisterMessageHandler(_activeHandler);
        }

        private static void UnregisterMessageHandler()
        {
            if (_activeHandler == null)
            {
                return;
            }
            RunManager.Instance.NetService.UnregisterMessageHandler(_activeHandler);
            _activeHandler = null;
        }

        private static void HandleTransferMessage(TransferMessage message, ulong _)
        {
            if (_playerCollection == null)
            {
                return;
            }
            Player? from = _playerCollection.GetPlayer(message.fromNetId);
            Player? to = _playerCollection.GetPlayer(message.toNetId);

            if (from == null || to == null)
            {
                return;
            }

            from.Gold -= message.amount;
            to.Gold += message.amount;
            
            Logger.Info("Message sent to clients");
        }

        public struct TransferMessage : INetMessage
        {
            public ulong fromNetId;
            public ulong toNetId;
            public int amount;
            
            public bool ShouldBroadcast => true;
            public NetTransferMode Mode => NetTransferMode.Reliable;
            public LogLevel LogLevel => LogLevel.Info;
            public bool ShouldBuffer => true;

            public void Serialize(PacketWriter writer)
            {
                writer.WriteULong(fromNetId);
                writer.WriteULong(toNetId);
                writer.WriteInt(amount);
            }

            public void Deserialize(PacketReader reader)
            {
                fromNetId = reader.ReadULong();
                toNetId = reader.ReadULong();
                amount = reader.ReadInt();
            }

            public override string ToString()
            {
                return $"{fromNetId} transferred {amount} gold to {toNetId}";
            }
        }
    }
}