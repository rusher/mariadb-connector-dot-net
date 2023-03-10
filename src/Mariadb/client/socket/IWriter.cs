namespace Mariadb.client.socket;

public interface IWriter
{
    Task WriteByte(int value);
    Task WriteShort(short value);
    Task WriteInt(int value);
    Task WriteUInt(uint value);
    Task WriteLong(long value);
    Task WriteDouble(double value);
    Task WriteFloat(float value);
    Task WriteBytes(byte[] arr);
    void WriteBytesAtPos(byte[] arr, int pos);
    Task WriteBytes(byte[] arr, int off, int len);
    Task WriteLength(long length);
    Task WriteAscii(string str);
    Task WriteString(string str);
    Task WriteStringEscaped(string str, bool noBackslashEscapes);
    Task WriteBytesEscaped(byte[] bytes, int len, bool noBackslashEscapes);
    Task WriteEmptyPacket();
    Task Flush();
    Task FlushPipeline();
    bool ThrowMaxAllowedLength(int length);
    long GetCmdLength();
    void PermitTrace(bool permitTrace);
    void SetServerThreadId(long? serverThreadId, HostAddress hostAddress);
    void Mark();
    bool IsMarked();
    bool HasFlushed();
    Task FlushBufferStopAtMark();
    bool BufIsDataAfterMark();
    byte[] ResetMark();
    void InitPacket(CancellationToken cancellationToken);
    void Close();
}