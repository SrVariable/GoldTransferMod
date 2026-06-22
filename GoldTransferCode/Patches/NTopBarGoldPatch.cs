using Godot;
using GoldTransfer.GoldTransferCode.UI;
using GoldTransfer.GoldTransferCode.Services;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace GoldTransfer.GoldTransferCode.Patches;

[HarmonyPatch(typeof(NTopBarGold))]
public class NTopBarGoldPatch
{
    [HarmonyPatch(nameof(NTopBarGold.Initialize))]
    [HarmonyPostfix]
    public static void Initialize(NTopBarGold __instance, Player player)
    {
        if (player.RunState.Players.Count == 1)
        {
            return;
        }

        Buttons.MakeSendButton(__instance, player, OnSendTransfer);
        GoldTransferService.RegisterMessageHandler(player.RunState);
    }
    
    [HarmonyPatch(nameof(NTopBarGold._ExitTree))]
    [HarmonyPostfix]
    public static void _ExitTree()
    {
        GoldTransferService.UnregisterMessageHandler();
    }
    
    private static void OnSendTransfer(NTopBarGold topBarGold, Player player)
    {
        if (player.Gold <= 0)
        {
            return;
        }

        List<Player> otherPlayers = player.RunState.Players
            .Where(p => !LocalContext.IsMe(p))
            .ToList();
        CanvasLayer popup = NSelectPlayerPopup.Create(player, otherPlayers, toPlayer =>
        {
            // TODO(srvariable): Open amount popup
            GoldTransferService.Transfer(player, toPlayer, 10);
        });
        topBarGold.GetTree().Root.AddChild(popup);
    }
}