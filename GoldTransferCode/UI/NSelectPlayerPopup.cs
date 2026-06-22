using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace GoldTransfer.GoldTransferCode.UI;

public static class NSelectPlayerPopup
{

    public static CanvasLayer Create(Player player, List<Player> otherPlayers, Action<Player>? onPressed)
    {
        CanvasLayer layer = new CanvasLayer();

        layer.AddChild(MakeOverlay());
        
        CenterContainer center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Pass };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        center.AddChild(MakePanel(layer, player, otherPlayers, onPressed));
        center.OffsetBottom = -200f; // To align the buttons to the center of the screen (more or less)
        layer.AddChild(center);
        
        return layer;
    }

    private static ColorRect MakeOverlay()
    {
        ColorRect overlay = new ColorRect { Color = Colors.Overlay, MouseFilter = Control.MouseFilterEnum.Ignore };
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
        title.AddThemeColorOverride("font_color", Colors.Gold);
        title.AddThemeFontSizeOverride("font_size", 32);

        return title;
    }

    private static Panel MakePanel(CanvasLayer layer, Player player, List<Player> otherPlayers, Action<Player>? onPressed)
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

        foreach (Player p in otherPlayers)
        {
            string playerName = PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform, p.NetId);
            LocString locString = new LocString("gameplay_ui", "GOLD_TRANSFER.PLAYER_NAME_AND_GOLD");
            locString.Add("playerName", playerName);
            locString.Add("amount", p.Gold);
            vbox.AddChild(Buttons.MakePlayerButton(locString.GetFormattedText(), toPlayer =>
            {
                layer.QueueFree();
                onPressed?.Invoke(toPlayer);
            }, p));
        }

        vbox.AddChild(Buttons.MakeCancelButton(layer));

        return panel;
    }
    
    private static StyleBoxFlat MakePanelStyle() => new()
    {
        BgColor = Colors.Panel,
        BorderColor = Colors.Border,
        BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
    };
    
    private static HSeparator MakeSeparator()
    {
        HSeparator sep = new HSeparator();
        sep.AddThemeStyleboxOverride("separator", new StyleBoxLine { Color = Colors.Border, Thickness = 1 });
        
        return sep;
    }
}