using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace GoldTransfer.GoldTransferCode.UI;

public static class Buttons
{
    public static void MakeSendButton(NTopBarGold topBarGold, Player player, Action<NTopBarGold, Player> onPressed)
    {
        Button btn2 = Buttons.MakeButton(
            new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_BUTTON").GetFormattedText(),
            Colors.SendBtn,
            Colors.Text,
            new Vector2(100, 60),
            Control.SizeFlags.ExpandFill, 
            Control.SizeFlags.ShrinkCenter,
            24);
            
        HoverTip hoverTip = new HoverTip(new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_TITLE"), new LocString("gameplay_ui", "GOLD_TRANSFER.SEND_DESCRIPTION"));
            
        btn2.MouseEntered += () => NHoverTipSet.CreateAndShow(btn2, hoverTip)?.SetGlobalPosition(btn2.GlobalPosition + new Vector2(0f, btn2.Size.Y + 20f));
        btn2.MouseExited += () => NHoverTipSet.Remove(btn2);
        btn2.Pressed += () => onPressed(topBarGold, player);

        topBarGold.AddChild(btn2);
    }
    
    public static Button MakePlayerButton(
        String text,
        Action<Player> onPressed,
        Player toPlayer)
    {
            Button btn = MakeButton(text, Colors.PlayerBtn);
            btn.Pressed += () => onPressed(toPlayer);

            return btn;
    }
            
    public static Button MakeCancelButton(CanvasLayer layer)
    {
        Button cancelBtn = MakeButton(new LocString("gameplay_ui", "GOLD_TRANSFER.CANCEL").GetFormattedText(), Colors.Cancel);
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

    public static Button MakeConfirmButton(CanvasLayer layer, Action onConfirm)
    {
        Button confirmBtn = MakeButton(new LocString("gameplay_ui", "GOLD_TRANSFER.CONFIRM_BUTTON").GetFormattedText(), Colors.Confirm);
        confirmBtn.Pressed += () =>
        {
            layer.QueueFree();
            onConfirm();
        };
        
        return confirmBtn;
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
        btn.AddThemeColorOverride("font_color", textColor ?? Colors.Text);
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