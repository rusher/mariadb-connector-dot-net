using Mariadb.client;
using Mariadb.client.socket;

namespace Mariadb.message.client;

public class QuitPacket : AbstractClientMessage
{
    /**
     * default instance to encode packet
     */
    public static QuitPacket INSTANCE = new();

    public override string Description => "QUIT";

    public override async Task<int> Encode(CancellationToken cancellationToken, IWriter writer, IContext context)
    {
        writer.InitPacket(cancellationToken);
        await writer.WriteByte(0x01);
        await writer.Flush();
        return 0;
    }
}