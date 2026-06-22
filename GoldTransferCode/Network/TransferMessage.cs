using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace GoldTransfer.GoldTransferCode.Network;

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