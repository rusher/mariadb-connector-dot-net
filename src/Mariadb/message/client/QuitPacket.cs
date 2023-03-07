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

    public override int Encode(IWriter writer, IContext context)
    {
        writer.InitPacket();
        writer.WriteByte(0x01);
        writer.Flush();
        return 0;
    }
}