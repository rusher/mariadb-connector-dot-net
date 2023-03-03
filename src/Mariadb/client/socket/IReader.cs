using Mariadb.client.util;

namespace Mariadb.client.socket;

public interface IReader
{
    
  IReadableByteBuf ReadReusablePacket(bool traceEnable);
  IReadableByteBuf ReadReusablePacket();
  byte[] ReadPacket(bool traceEnable);
  IReadableByteBuf ReadableBufFromArray(byte[] buf);
  void SkipPacket() ;
  MutableByte GetSequence();
  void Close() ;
  void SetServerThreadId(long serverThreadId, HostAddress hostAddress);

}