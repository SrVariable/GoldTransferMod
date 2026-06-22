using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace GoldTransfer.GoldTransferCode;

//You're recommended but not required to keep all your code in this package and all your assets in the GoldTransfer folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "GoldTransfer"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, LogType.Generic);
    
    private static readonly Color ColorOverlay = new(0f, 0f, 0f, 0.9f);
    private static readonly Color ColorPanel = new(0.11f, 0.17f, 0.22f);
    private static readonly Color ColorBorder = new(0.937f, 0.784f, 0.317f);
    private static readonly Color ColorGold = ColorBorder;
    private static readonly Color ColorPlayerBtn = new(0.15f, 0.28f, 0.38f);
    private static readonly Color ColorCancel = new(0.55f, 0.15f, 0.15f);
    private static readonly Color ColorText = new(1f, 1f, 1f);
    private static readonly Color ColorSendBtn = new Color (0.231f, 0.565f, 0.584f);
    
    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(ModId);

        harmony.PatchAll();
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
            _playerCollection = player.RunState;

            if (_playerCollection.Players.Count == 1)
            {
                return;
            }
            
            AddSendButton(__instance, player);
            RegisterMessageHandler();
        }
        
        [HarmonyPatch(nameof(NTopBarGold._ExitTree))]
        [HarmonyPostfix]
        public static void _ExitTree()
        {
            UnregisterMessageHandler();
            _playerCollection = null;
        }

        private static void AddSendButton(NTopBarGold topBarGold, Player player)
        {
            Button btn2 = MakeButton(
                new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_BUTTON").GetFormattedText(),
                ColorSendBtn,
                ColorText,
                new Vector2(100, 60),
                Control.SizeFlags.ExpandFill, 
                Control.SizeFlags.ShrinkCenter,
                24);
            
            HoverTip hoverTip = new HoverTip(new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_TITLE"), new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_DESCRIPTION"));
            
            btn2.MouseEntered += () => NHoverTipSet.CreateAndShow(btn2, hoverTip)?.SetGlobalPosition(btn2.GlobalPosition + new Vector2(0f, btn2.Size.Y + 20f));
            btn2.MouseExited += () => NHoverTipSet.Remove(btn2);
            btn2.Pressed += () => OnSendTransfer(topBarGold, player);

            topBarGold.AddChild(btn2);
        }

        private static void OnSendTransfer(NTopBarGold topBarGold, Player player)
        {
            if (player.Gold <= 0)
            {
                return;
            }

            CanvasLayer popup = NSelectPlayerPopup.Create(player, null);
            topBarGold.GetTree().Root.AddChild(popup);
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
                FromNetId = player.NetId,
                ToNetId = toPlayer.NetId,
                Amount = amount
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

            Player? from = _playerCollection.GetPlayer(message.FromNetId);
            Player? to = _playerCollection.GetPlayer(message.ToNetId);
            if (from == null || to == null)
            {
                return;
            }

            from.Gold -= message.Amount;
            to.Gold += message.Amount;
            
            Logger.Info("Message sent to clients");
        }

        public struct TransferMessage : INetMessage
        {
            public ulong FromNetId;
            public ulong ToNetId;
            public int Amount;

            public bool ShouldBroadcast => true;
            public NetTransferMode Mode => NetTransferMode.Reliable;
            public LogLevel LogLevel => LogLevel.Info;
            public bool ShouldBuffer => true;

            public void Serialize(PacketWriter writer)
            {
                writer.WriteULong(FromNetId);
                writer.WriteULong(ToNetId);
                writer.WriteInt(Amount);
            }

            public void Deserialize(PacketReader reader)
            {
                FromNetId = reader.ReadULong();
                ToNetId = reader.ReadULong();
                Amount = reader.ReadInt();
            }

            public override string ToString()
            {
                return $"{FromNetId} transferred {Amount} gold to {ToNetId}";
            }
        }

        public static class NSelectPlayerPopup
        {

            public static CanvasLayer Create(Player player, Action<Player>? onPlayerSelected)
            {
                CanvasLayer layer = new CanvasLayer();

                layer.AddChild(MakeOverlay());
                
                CenterContainer center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Pass };
                center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                center.AddChild(MakePanel(layer, player, onPlayerSelected));
                center.OffsetBottom = -200f; // To align the buttons to the center of the screen (more or less)
                layer.AddChild(center);
                
                return layer;
            }

            private static ColorRect MakeOverlay()
            {
                ColorRect overlay = new ColorRect { Color = ColorOverlay, MouseFilter = Control.MouseFilterEnum.Ignore };
                overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

                return overlay;
            }
            
            private static Label MakeTitle()
            {
                Label title = new Label
                {
                    Text = new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_TITLE").GetFormattedText(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                title.AddThemeColorOverride("font_color", ColorGold);
                title.AddThemeFontSizeOverride("font_size", 32);

                return title;
            }

            private static Panel MakePanel(CanvasLayer layer, Player player, Action<Player>? onPlayerSelected)
            {
                Panel panel = new Panel { CustomMinimumSize = new Vector2(340, 0) };
                panel.AddThemeStyleboxOverride("panel", MakePanelStyle());

                MarginContainer margin = new MarginContainer();
                margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                foreach (var (side, val) in new[] {("margin_left", 20), ("margin_right", 20), ("margin_top", 18), ("margin_bottom", 18)})
                {
                    margin.AddThemeConstantOverride(side, val);
                }
                panel.AddChild(margin);

                VBoxContainer vbox = new VBoxContainer();
                vbox.AddThemeConstantOverride("separation", 10);
                margin.AddChild(vbox);
                
                vbox.AddChild(MakeTitle());
                vbox.AddChild(MakeSeparator());

                foreach (Button btn in MakePlayerButtons(layer, player, onPlayerSelected))
                {
                    vbox.AddChild(btn);
                }

                vbox.AddChild(MakeCancelButton(layer));

                return panel;
            }

            private static IEnumerable<Button> MakePlayerButtons(
                CanvasLayer layer,
                Player player,
                Action<Player>? onPlayerSelected)
            {
                List<Player> otherPlayers = player.RunState.Players
                    .Where(p => !LocalContext.IsMe(p))
                    .ToList();
                foreach (Player p in otherPlayers)
                {
                    string playerName =
                        PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform,
                            p.NetId);
                    LocString locString = new LocString("gameplay_ui",
                        "GOLD_TRANSFER.PLAYER_NAME_AND_GOLD");
                    locString.Add("playerName", playerName);
                    locString.Add("amount", p.Gold);
                    Button btn = MakeButton(locString.GetFormattedText(), ColorPlayerBtn);
                    btn.Pressed += () =>
                    {
                        layer.QueueFree();
                        onPlayerSelected?.Invoke(p);
                        Transfer(player, p, 10);
                    };

                    yield return btn;
                }
            }
            
            private static Button MakeCancelButton(CanvasLayer layer)
            {
                Button cancelBtn = MakeButton(new LocString("gameplay_ui", "GOLD_TRANSFER.CANCEL").GetFormattedText(), ColorCancel);
                Shortcut shortcut = new Shortcut();
                shortcut.Events = new Godot.Collections.Array
                {
                    new InputEventKey
                    {
                        Keycode = Key.Escape,
                    }
                };
                cancelBtn.Pressed += layer.QueueFree;
                cancelBtn.Shortcut = shortcut;

                return cancelBtn;
            }
            
            private static StyleBoxFlat MakePanelStyle() => new()
            {
                BgColor = ColorPanel,
                BorderColor = ColorBorder,
                BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
            };
            
            private static HSeparator MakeSeparator()
            {
                HSeparator sep = new HSeparator();
                sep.AddThemeStyleboxOverride("separator", new StyleBoxLine { Color = ColorBorder, Thickness = 1 });
                
                return sep;
            }
            
        }
        
        private static Button MakeButton(
            string text,
            Color bgColor,
            Color? textColor = null,
            Vector2? customMinimumSize = null,
            Control.SizeFlags sizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Control.SizeFlags sizeFlagsVertical = Control.SizeFlags.ExpandFill,
            int fontSize = 20)
        {
            Button btn = new Button
            {
                Text = text,
                CustomMinimumSize = customMinimumSize ?? new Vector2(0, 44),
                SizeFlagsHorizontal = sizeFlagsHorizontal,
                SizeFlagsVertical = sizeFlagsVertical
            };
            btn.AddThemeStyleboxOverride("normal", MakeButtonStyle(bgColor));
            btn.AddThemeStyleboxOverride("hover", MakeButtonStyle(bgColor.Lightened(0.15f)));
            btn.AddThemeStyleboxOverride("pressed", MakeButtonStyle(bgColor.Darkened(0.10f)));
            btn.AddThemeColorOverride("font_color", textColor ?? ColorText);
            btn.AddThemeFontSizeOverride("font_size", fontSize);
            btn.AddThemeConstantOverride("outline_size", 2);
            
            return btn;
        }

        private static StyleBoxFlat MakeButtonStyle(Color color) => new()
        {
            BgColor = color,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
    }
}