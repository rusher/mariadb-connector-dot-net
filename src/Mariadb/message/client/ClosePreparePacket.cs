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

    public override async Task<int> Encode(CancellationToken cancellationToken, IWriter writer, IContext context)
    {
        writer.InitPacket(cancellationToken);
        await writer.WriteByte(0x19);
        await writer.WriteUInt(_statementId);
        await writer.Flush();
        return 0;
    }
}