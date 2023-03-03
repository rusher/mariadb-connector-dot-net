using Mariadb.client;
using Mariadb.client.util;
using Mariadb.utils.constant;
using Mariadb.utils.log;

namespace Mariadb.message.server;

public class OkPacket : ICompletion
{
  private static Ilogger logger = Loggers.getLogger("OkPacket");
  
  public readonly long AffectedRows;
  public readonly long LastInsertId;

  public OkPacket(IReadableByteBuf buf, IContext context) {
    buf.Skip(); // ok header
    AffectedRows = buf.ReadLongLengthEncodedNotNull();
    LastInsertId = buf.ReadLongLengthEncodedNotNull();
    context.setServerStatus(buf.ReadUnsignedShort());
    context.setWarning(buf.ReadUnsignedShort());

    if (buf.ReadableBytes() > 0 && context.hasClientCapability(Capabilities.CLIENT_SESSION_TRACK)) {
      buf.Skip(buf.ReadIntLengthEncodedNotNull()); // skip info
      while (buf.ReadableBytes() > 0) {
        if (buf.ReadIntLengthEncodedNotNull() > 0) {
          switch (Convert.ToUInt16(buf.ReadByte())) {
            case StateChange.SESSION_TRACK_SYSTEM_VARIABLES:
              buf.ReadIntLengthEncodedNotNull();
              string variable = buf.ReadString(buf.ReadIntLengthEncodedNotNull());
              int? len = buf.ReadLength();
              string value = len == null ? null : buf.ReadString(len.Value);
              logger.debug($"System variable change:  {variable} = {value}");
              break;

            case StateChange.SESSION_TRACK_SCHEMA:
              buf.ReadIntLengthEncodedNotNull();
              int? dbLen = buf.ReadLength();
              string? database = dbLen == null ? null : buf.ReadString(dbLen.Value);
              context.setDatabase(String.IsNullOrEmpty(database) ? null : database);
              logger.debug($"Database change: is '{database}'");
              break;

            default:
              buf.Skip(buf.ReadIntLengthEncodedNotNull());
              break;
          }
        }
      }
    }
  }
}