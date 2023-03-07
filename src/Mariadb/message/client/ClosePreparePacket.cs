using Mariadb.client;
using Mariadb.client.socket;

namespace Mariadb.message.client;

public class ClosePreparePacket : AbstractClientMessage
{
    private readonly uint _statementId;

    public ClosePreparePacket(uint statementId)
    {
        _statementId = statementId;
    }

    public override string Description => "Closing PREPARE " + _statementId;

    public override int Encode(IWriter writer, IContext context)
    {
        writer.InitPacket();
        writer.WriteByte(0x19);
        writer.WriteUInt(_statementId);
        writer.Flush();
        return 0;
    }
}