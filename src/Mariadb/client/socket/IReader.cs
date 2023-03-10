using Mariadb.client.util;

namespace Mariadb.client.socket;

public interface IReader
{
    Task<IReadableByteBuf> ReadReusablePacket(CancellationToken cancellationToken, bool traceEnable);
    Task<IReadableByteBuf> ReadReusablePacket(CancellationToken cancellationToken);
    Task<byte[]> ReadPacket(CancellationToken cancellationToken, bool traceEnable);
    IReadableByteBuf ReadableBufFromArray(byte[] buf);
    Task SkipPacket(CancellationToken cancellationToken);
    MutableByte GetSequence();
    void Close();
    void SetServerThreadId(long? serverThreadId, HostAddress hostAddress);
}