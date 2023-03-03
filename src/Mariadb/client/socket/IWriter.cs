namespace Mariadb.client.socket;

public interface IWriter
{
    void WriteByte(int value);
    void WriteShort(short value);
    void WriteInt(int value);
    void WriteLong(long value);
    void WriteDouble(double value);
    void WriteFloat(float value);
    void WriteBytes(byte[] arr);
    void WriteBytesAtPos(byte[] arr, int pos);
    void WriteBytes(byte[] arr, int off, int len);
    void WriteLength(long length);
    void WriteAscii(string str);
    void WriteString(string str);
    void WriteStringEscaped(string str, bool noBackslashEscapes);
    void WriteBytesEscaped(byte[] bytes, int len, bool noBackslashEscapes);
    void WriteEmptyPacket();
    void Flush();
    void FlushPipeline();
    bool ThrowMaxAllowedLength(int length);
    long GetCmdLength();
    void PermitTrace(bool permitTrace);
    void SetServerThreadId(long serverThreadId, HostAddress hostAddress);
    void Mark();
    bool IsMarked();
    bool HasFlushed();
    void FlushBufferStopAtMark();
    bool BufIsDataAfterMark();
    byte[] ResetMark();
    void InitPacket();
    void Close();
}