using Mariadb.client;
using Mariadb.client.util;
using Mariadb.utils.constant;
using Mariadb.utils.log;

namespace Mariadb.message.server;

public class ErrorPacket: ICompletion
{
    
    private static Ilogger logger = Loggers.getLogger("ErrorPacket");
  
    public readonly ushort ErrorCode;
    public readonly string Message;
    public readonly string SqlState;

    public ErrorPacket(IReadableByteBuf buf, IContext context) {
        buf.Skip();
        ErrorCode = buf.ReadUnsignedShort();
        byte next = buf.GetByte();
        if (next == '#') {
            buf.Skip(); // skip '#'
            SqlState = buf.ReadAscii(5);
            Message = buf.ReadStringEof();
        } else {
            // Pre-4.1 message, still can be output in newer versions (e.g. with 'Too many connections')
            Message = buf.ReadStringEof();
            SqlState = "HY000";
        }
        if (logger.isWarnEnabled()) {
            logger.warn($"Error: {ErrorCode}-{SqlState}: {Message}");
        }

        // force current status to in transaction to ensure rollback/commit, since command may have
        // issue a transaction
        if (context != null) {
            int serverStatus = context.getServerStatus();
            serverStatus |= ServerStatus.IN_TRANSACTION;
            context.setServerStatus(serverStatus);
        }
    }

}