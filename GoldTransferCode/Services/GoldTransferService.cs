using GoldTransfer.GoldTransferCode.Network;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace GoldTransfer.GoldTransferCode.Services;

public static class GoldTransferService
{
    private static MessageHandlerDelegate<TransferMessage>? _activeHandler;
    private static IPlayerCollection? _playerCollection;

    public static void Transfer(Player player, Player? toPlayer, int amount)
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
        MainFile.Logger.Info($"{player.NetId}: {player.Gold}");
        MainFile.Logger.Info($"{toPlayer.NetId}: {toPlayer.Gold}");

        RunManager.Instance.NetService.SendMessage(new TransferMessage
        {
            FromNetId = player.NetId,
            ToNetId = toPlayer.NetId,
            Amount = amount
        });
    }

    public static void RegisterMessageHandler(IPlayerCollection playerCollection)
    {
        _playerCollection = playerCollection;
        _activeHandler = HandleTransferMessage;
        RunManager.Instance.NetService.RegisterMessageHandler(_activeHandler);
    }

    public static void UnregisterMessageHandler()
    {
        if (_activeHandler == null)
        {
            return;
        }

        RunManager.Instance.NetService.UnregisterMessageHandler(_activeHandler);
        _activeHandler = null;
        _playerCollection = null;
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
        
        MainFile.Logger.Info("Message sent to clients");
    }
}