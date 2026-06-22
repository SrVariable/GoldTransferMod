using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace GoldTransfer.GoldTransferCode.UI;

public static class NAmountPopup
{
    public static CanvasLayer Create(Player toPlayer, int maxGold, Action<int> onConfirm)
    {
        CanvasLayer layer = new CanvasLayer{ Layer = 128 };

        layer.AddChild(MakeOverlay());
        CenterContainer center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Pass };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        center.AddChild(MakePanel(layer, toPlayer, maxGold / 2, maxGold, onConfirm));
        center.OffsetBottom = -200f; // To align the buttons to the center of the screen (more or less)
        layer.AddChild(center);
        
        return layer;
    }
    
    private static ColorRect MakeOverlay()
    {
        ColorRect overlay = new ColorRect { Color = Colors.Overlay, MouseFilter = Control.MouseFilterEnum.Stop };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        return overlay;
    }

    private static Panel MakePanel(CanvasLayer layer, Player toPlayer, int selectedAmount, int maxGold, Action<int> onConfirm)
    {
        Panel panel = new Panel { CustomMinimumSize = new Vector2(420, 0) };
        panel.AddThemeStyleboxOverride("panel", MakePanelStyle());
        
        MarginContainer margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (var (side, val) in new[] { ("margin_left", 24), ("margin_right", 24), ("margin_top", 20), ("margin_bottom", 20) })
            margin.AddThemeConstantOverride(side, val);
        panel.AddChild(margin);

        VBoxContainer vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        margin.AddChild(vbox);

        string toPlayerName = PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform, toPlayer.NetId);
        vbox.AddChild(MakeTitle(toPlayerName));

        Label amountLabel = MakeAmountLabel(selectedAmount);
        vbox.AddChild(amountLabel);
        
        vbox.AddChild(MakeSlider(maxGold, selectedAmount, amount =>
        {
            selectedAmount = (int)amount;
            LocString locString = new LocString("gameplay_ui", "GOLD_TRANSFER.SELECTED_AMOUNT");
            locString.Add("selectedAmount", selectedAmount);
            amountLabel.Text = locString.GetFormattedText();
        }));
        
        vbox.AddChild(MakeHint(selectedAmount));
        
        HBoxContainer row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 14);
        vbox.AddChild(row);

        row.AddChild(Buttons.MakeCancelButton(layer));
        row.AddChild(Buttons.MakeConfirmButton(layer, () =>
        {
            if (selectedAmount > 0)
            {
                onConfirm(selectedAmount);
            }
            layer.QueueFree();
        }));

        return panel;
    }

    private static Label MakeTitle(string toPlayerName)
    {
        LocString locString = new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_GOLD_TO_PLAYER");
        locString.Add("playerName", toPlayerName);

        Label title = new Label { Text = locString.GetFormattedText(), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeColorOverride("font_color", Colors.Gold);
        title.AddThemeFontSizeOverride("font_size", 20);

        return title;
    }

    private static Label MakeAmountLabel(int selectedAmount)
    {
        LocString locString = new LocString("gameplay_ui", "GOLD_TRANSFER.SELECTED_AMOUNT");
        locString.Add("selectedAmount", selectedAmount);

        Label amountLabel = new Label { Text = locString.GetFormattedText(), HorizontalAlignment = HorizontalAlignment.Center };
        amountLabel.AddThemeColorOverride("font_color", Colors.Text);
        amountLabel.AddThemeFontSizeOverride("font_size", 30);

        return amountLabel;
    }

    private static HSlider MakeSlider(int maxGold, int selectedAmount, Action<double> onValueChanged)
    {
        HSlider slider = new HSlider
        {
            MinValue = 0, MaxValue = maxGold, Value = selectedAmount, Step = 1,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        slider.ValueChanged += amount => onValueChanged((int)amount);

        return slider;
    }

    private static Label MakeHint(int selectedAmount)
    {
        LocString locString = new LocString("gameplay_ui", "GOLD_TRANSFER.GOLD_AVAILABLE");
        locString.Add("maxGold", selectedAmount);

        Label hint = new Label { Text = locString.GetFormattedText(), HorizontalAlignment = HorizontalAlignment.Center };
        hint.AddThemeColorOverride("font_color", Colors.Muted);
        hint.AddThemeFontSizeOverride("font_size", 13);

        return hint;
    }
    
    private static StyleBoxFlat MakePanelStyle() => new()
    {
        BgColor = Colors.Panel,
        BorderColor = Colors.Border,
        BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
    };
}