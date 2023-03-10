using System.Text;
using Mariadb.client.util;
using Mariadb.utils.exception;
using Mariadb.utils.log;

namespace Mariadb.client.socket;

public class PacketWriter : IWriter
{
    /**
     * initial buffer size
     */
    public static int SMALL_BUFFER_SIZE = 8192;

    private static readonly Ilogger logger = Loggers.getLogger("PacketWriter");
    private static readonly byte QUOTE = (byte)'\'';
    private static readonly byte DBL_QUOTE = (byte)'"';
    private static readonly byte ZERO_BYTE = (byte)'\0';
    private static readonly byte BACKSLASH = (byte)'\\';
    private static readonly int MEDIUM_BUFFER_SIZE = 128 * 1024;
    private static readonly int LARGE_BUFFER_SIZE = 1024 * 1024;
    private static readonly int MAX_PACKET_LENGTH = 0x00ffffff + 4;
    private readonly uint? _maxAllowedPacket;
    private readonly int _maxPacketLength = MAX_PACKET_LENGTH;
    private readonly uint _maxQuerySizeToLog;
    private readonly Stream _out;
    private readonly MutableByte _sequence;

    private bool _bufContainDataAfterMark;
    private CancellationToken _cancellationToken = CancellationToken.None;
    private long _cmdLength;
    protected MutableByte _compressSequence;
    private int _mark = -1;
    private bool _permitTrace = true;
    private int _pos = 4;
    private string _serverThreadLog = "";

    public PacketWriter(
        Stream stream,
        uint maxQuerySizeToLog,
        uint? maxAllowedPacket,
        MutableByte sequence,
        MutableByte compressSequence)
    {
        _out = stream;
        Buf = new byte[SMALL_BUFFER_SIZE];
        _maxQuerySizeToLog = maxQuerySizeToLog;
        _cmdLength = 0;
        _sequence = sequence;
        _compressSequence = compressSequence;
        _maxAllowedPacket = maxAllowedPacket;
    }

    public int Pos
    {
        get => _pos;
        set
        {
            if (value > Buf.Length) GrowBuffer(value);
            _pos = value;
        }
    }

    public byte[] Buf { get; private set; }

    public long GetCmdLength()
    {
        return _cmdLength;
    }

    public async Task WriteByte(int value)
    {
        if (_pos >= Buf.Length)
        {
            if (_pos >= _maxPacketLength && !_bufContainDataAfterMark)
                // buf is more than a Packet, must flushbuf()
                await WriteSocket(false);
            else
                await GrowBuffer(1);
        }

        Buf[_pos++] = (byte)value;
    }

    public async Task WriteShort(short value)
    {
        if (2 > Buf.Length - _pos)
        {
            // not enough space remaining
            await WriteBytes(new[] { (byte)value, (byte)(value >> 8) }, 0, 2);
            return;
        }

        Buf[_pos] = (byte)value;
        Buf[_pos + 1] = (byte)(value >> 8);
        _pos += 2;
    }

    public async Task WriteInt(int value)
    {
        if (4 > Buf.Length - Pos)
        {
            // not enough space remaining
            var arr = new byte[4];
            arr[0] = (byte)value;
            arr[1] = (byte)(value >> 8);
            arr[2] = (byte)(value >> 16);
            arr[3] = (byte)(value >> 24);
            await WriteBytes(arr, 0, 4);
            return;
        }

        Buf[_pos] = (byte)value;
        Buf[_pos + 1] = (byte)(value >> 8);
        Buf[_pos + 2] = (byte)(value >> 16);
        Buf[_pos + 3] = (byte)(value >> 24);
        _pos += 4;
    }

    public async Task WriteUInt(uint value)
    {
        if (4 > Buf.Length - Pos)
        {
            // not enough space remaining
            var arr = new byte[4];
            arr[0] = (byte)value;
            arr[1] = (byte)(value >> 8);
            arr[2] = (byte)(value >> 16);
            arr[3] = (byte)(value >> 24);
            await WriteBytes(arr, 0, 4);
            return;
        }

        Buf[_pos] = (byte)value;
        Buf[_pos + 1] = (byte)(value >> 8);
        Buf[_pos + 2] = (byte)(value >> 16);
        Buf[_pos + 3] = (byte)(value >> 24);
        _pos += 4;
    }

    public async Task WriteLong(long value)
    {
        if (8 > Buf.Length - _pos)
        {
            // not enough space remaining
            var arr = new byte[8];
            arr[0] = (byte)value;
            arr[1] = (byte)(value >> 8);
            arr[2] = (byte)(value >> 16);
            arr[3] = (byte)(value >> 24);
            arr[4] = (byte)(value >> 32);
            arr[5] = (byte)(value >> 40);
            arr[6] = (byte)(value >> 48);
            arr[7] = (byte)(value >> 56);
            await WriteBytes(arr, 0, 8);
            return;
        }

        Buf[_pos] = (byte)value;
        Buf[_pos + 1] = (byte)(value >> 8);
        Buf[_pos + 2] = (byte)(value >> 16);
        Buf[_pos + 3] = (byte)(value >> 24);
        Buf[_pos + 4] = (byte)(value >> 32);
        Buf[_pos + 5] = (byte)(value >> 40);
        Buf[_pos + 6] = (byte)(value >> 48);
        Buf[_pos + 7] = (byte)(value >> 56);
        _pos += 8;
    }

    public async Task WriteDouble(double value)
    {
        await WriteBytes(BitConverter.GetBytes(value), 0, 8);
    }

    public async Task WriteFloat(float value)
    {
        await WriteBytes(BitConverter.GetBytes(value), 0, 4);
    }

    public async Task WriteBytes(byte[] arr)
    {
        await WriteBytes(arr, 0, arr.Length);
    }

    public void WriteBytesAtPos(byte[] arr, int pos)
    {
        Array.Copy(arr, 0, Buf, pos, arr.Length);
    }

    public async Task WriteBytes(byte[] arr, int off, int len)
    {
        if (len > Buf.Length - _pos)
        {
            if (Buf.Length != _maxPacketLength) await GrowBuffer(len);

            // max buf size
            if (len > Buf.Length - _pos)
            {
                if (_mark != -1)
                {
                    await GrowBuffer(len);
                    if (_mark != -1) await FlushBufferStopAtMark();
                }

                if (len > Buf.Length - _pos)
                {
                    // not enough space in buf, will stream :
                    // fill buf and flush until all data are snd
                    var remainingLen = len;
                    do
                    {
                        var lenToFillbuf = Math.Min(_maxPacketLength - _pos, remainingLen);
                        Array.Copy(arr, off, Buf, _pos, lenToFillbuf);
                        remainingLen -= lenToFillbuf;
                        off += lenToFillbuf;
                        _pos += lenToFillbuf;
                        if (remainingLen > 0)
                            await WriteSocket(false);
                        else
                            break;
                    } while (true);

                    return;
                }
            }
        }

        Array.Copy(arr, off, Buf, _pos, len);
        _pos += len;
    }

    public async Task WriteLength(long length)
    {
        if (length < 251)
        {
            await WriteByte((byte)length);
            return;
        }

        if (length < 65536)
        {
            if (3 > Buf.Length - _pos)
            {
                // not enough space remaining
                var arr = new byte[3];
                arr[0] = 0xfc;
                arr[1] = (byte)length;
                arr[2] = (byte)(length >>> 8);
                await WriteBytes(arr, 0, 3);
                return;
            }

            Buf[_pos] = 0xfc;
            Buf[_pos + 1] = (byte)length;
            Buf[_pos + 2] = (byte)(length >>> 8);
            _pos += 3;
            return;
        }

        if (length < 16777216)
        {
            if (4 > Buf.Length - _pos)
            {
                // not enough space remaining
                var arr = new byte[4];
                arr[0] = 0xfd;
                arr[1] = (byte)length;
                arr[2] = (byte)(length >>> 8);
                arr[3] = (byte)(length >>> 16);
                await WriteBytes(arr, 0, 4);
                return;
            }

            Buf[_pos] = 0xfd;
            Buf[_pos + 1] = (byte)length;
            Buf[_pos + 2] = (byte)(length >>> 8);
            Buf[_pos + 3] = (byte)(length >>> 16);
            _pos += 4;
            return;
        }

        if (9 > Buf.Length - _pos)
        {
            // not enough space remaining
            var arr = new byte[9];
            arr[0] = 0xfe;
            arr[1] = (byte)length;
            arr[2] = (byte)(length >>> 8);
            arr[3] = (byte)(length >>> 16);
            arr[4] = (byte)(length >>> 24);
            arr[5] = (byte)(length >>> 32);
            arr[6] = (byte)(length >>> 40);
            arr[7] = (byte)(length >>> 48);
            arr[8] = (byte)(length >>> 56);
            await WriteBytes(arr, 0, 9);
            return;
        }

        Buf[_pos] = 0xfe;
        Buf[_pos + 1] = (byte)length;
        Buf[_pos + 2] = (byte)(length >>> 8);
        Buf[_pos + 3] = (byte)(length >>> 16);
        Buf[_pos + 4] = (byte)(length >>> 24);
        Buf[_pos + 5] = (byte)(length >>> 32);
        Buf[_pos + 6] = (byte)(length >>> 40);
        Buf[_pos + 7] = (byte)(length >>> 48);
        Buf[_pos + 8] = (byte)(length >>> 56);
        _pos += 9;
    }

    public async Task WriteAscii(string str)
    {
        var len = str.Length;
        if (len > Buf.Length - _pos)
        {
            var arr = Encoding.ASCII.GetBytes(str);
            await WriteBytes(arr, 0, arr.Length);
            return;
        }

        for (var off = 0; off < len;) Buf[_pos++] = (byte)str[off++];
    }

    public async Task WriteString(string str)
    {
        var charsLength = str.Length;

        // not enough space remaining
        if (charsLength * 3 >= Buf.Length - _pos)
        {
            var arr = Encoding.UTF8.GetBytes(str);
            await WriteBytes(arr, 0, arr.Length);
            return;
        }

        // create UTF-8 byte array
        // since java char are internally using UTF-16 using surrogate's pattern, 4 bytes unicode
        // characters will
        // represent 2 characters : example "\uD83C\uDFA4" = 🎤 unicode 8 "no microphones"
        // so max size is 3 * charLength
        // (escape characters are 1 byte encoded, so length might only be 2 when escape)
        // + 2 for the quotes for text protocol
        var charsOffset = 0;
        char currChar;

        // quick loop if only ASCII chars for faster escape
        for (;
             charsOffset < charsLength && (currChar = str[charsOffset]) < 0x80;
             charsOffset++)
            Buf[_pos++] = (byte)currChar;

        // if quick loop not finished
        while (charsOffset < charsLength)
        {
            currChar = str[charsOffset++];
            if (currChar < 0x80)
            {
                Buf[_pos++] = (byte)currChar;
            }
            else if (currChar < 0x800)
            {
                Buf[_pos++] = (byte)(0xc0 | (currChar >> 6));
                Buf[_pos++] = (byte)(0x80 | (currChar & 0x3f));
            }
            else if (currChar >= 0xD800 && currChar < 0xE000)
            {
                // reserved for surrogate - see https://en.wikipedia.org/wiki/UTF-16
                if (currChar < 0xDC00)
                {
                    // is high surrogate
                    if (charsOffset + 1 > charsLength)
                    {
                        Buf[_pos++] = 0x63;
                    }
                    else
                    {
                        var nextChar = str[charsOffset];
                        if (nextChar >= 0xDC00 && nextChar < 0xE000)
                        {
                            // is low surrogate
                            var surrogatePairs =
                                (currChar << 10) + nextChar + (0x010000 - (0xD800 << 10) - 0xDC00);
                            Buf[_pos++] = (byte)(0xf0 | (surrogatePairs >> 18));
                            Buf[_pos++] = (byte)(0x80 | ((surrogatePairs >> 12) & 0x3f));
                            Buf[_pos++] = (byte)(0x80 | ((surrogatePairs >> 6) & 0x3f));
                            Buf[_pos++] = (byte)(0x80 | (surrogatePairs & 0x3f));
                            charsOffset++;
                        }
                        else
                        {
                            // must have low surrogate
                            Buf[_pos++] = 0x3f;
                        }
                    }
                }
                else
                {
                    // low surrogate without high surrogate before
                    Buf[_pos++] = 0x3f;
                }
            }
            else
            {
                Buf[_pos++] = (byte)(0xe0 | (currChar >> 12));
                Buf[_pos++] = (byte)(0x80 | ((currChar >> 6) & 0x3f));
                Buf[_pos++] = (byte)(0x80 | (currChar & 0x3f));
            }
        }
    }

    public async Task WriteStringEscaped(string str, bool noBackslashEscapes)
    {
        var charsLength = str.Length;

        // not enough space remaining
        if (charsLength * 3 >= Buf.Length - _pos)
        {
            var arr = Encoding.UTF8.GetBytes(str);
            await WriteBytesEscaped(arr, arr.Length, noBackslashEscapes);
            return;
        }

        // create UTF-8 byte array
        // since java char are internally using UTF-16 using surrogate's pattern, 4 bytes unicode
        // characters will
        // represent 2 characters : example "\uD83C\uDFA4" = 🎤 unicode 8 "no microphones"
        // so max size is 3 * charLength
        // (escape characters are 1 byte encoded, so length might only be 2 when escape)
        // + 2 for the quotes for text protocol
        var charsOffset = 0;
        char currChar;

        // quick loop if only ASCII chars for faster escape
        if (noBackslashEscapes)
            for (;
                 charsOffset < charsLength && (currChar = str[charsOffset]) < 0x80;
                 charsOffset++)
            {
                if (currChar == QUOTE) Buf[_pos++] = QUOTE;
                Buf[_pos++] = (byte)currChar;
            }
        else
            for (;
                 charsOffset < charsLength && (currChar = str[charsOffset]) < 0x80;
                 charsOffset++)
            {
                if (currChar == BACKSLASH || currChar == QUOTE || currChar == 0 || currChar == DBL_QUOTE)
                    Buf[_pos++] = BACKSLASH;
                Buf[_pos++] = (byte)currChar;
            }

        // if quick loop not finished
        while (charsOffset < charsLength)
        {
            currChar = str[charsOffset++];
            if (currChar < 0x80)
            {
                if (noBackslashEscapes)
                {
                    if (currChar == QUOTE) Buf[_pos++] = QUOTE;
                }
                else if (currChar == BACKSLASH
                         || currChar == QUOTE
                         || currChar == ZERO_BYTE
                         || currChar == DBL_QUOTE)
                {
                    Buf[_pos++] = BACKSLASH;
                }

                Buf[_pos++] = (byte)currChar;
            }
            else if (currChar < 0x800)
            {
                Buf[_pos++] = (byte)(0xc0 | (currChar >> 6));
                Buf[_pos++] = (byte)(0x80 | (currChar & 0x3f));
            }
            else if (currChar >= 0xD800 && currChar < 0xE000)
            {
                // reserved for surrogate - see https://en.wikipedia.org/wiki/UTF-16
                if (currChar < 0xDC00)
                {
                    // is high surrogate
                    if (charsOffset + 1 > charsLength)
                    {
                        Buf[_pos++] = 0x63;
                    }
                    else
                    {
                        var nextChar = str[charsOffset];
                        if (nextChar >= 0xDC00 && nextChar < 0xE000)
                        {
                            // is low surrogate
                            var surrogatePairs =
                                (currChar << 10) + nextChar + (0x010000 - (0xD800 << 10) - 0xDC00);
                            Buf[_pos++] = (byte)(0xf0 | (surrogatePairs >> 18));
                            Buf[_pos++] = (byte)(0x80 | ((surrogatePairs >> 12) & 0x3f));
                            Buf[_pos++] = (byte)(0x80 | ((surrogatePairs >> 6) & 0x3f));
                            Buf[_pos++] = (byte)(0x80 | (surrogatePairs & 0x3f));
                            charsOffset++;
                        }
                        else
                        {
                            // must have low surrogate
                            Buf[_pos++] = 0x3f;
                        }
                    }
                }
                else
                {
                    // low surrogate without high surrogate before
                    Buf[_pos++] = 0x3f;
                }
            }
            else
            {
                Buf[_pos++] = (byte)(0xe0 | (currChar >> 12));
                Buf[_pos++] = (byte)(0x80 | ((currChar >> 6) & 0x3f));
                Buf[_pos++] = (byte)(0x80 | (currChar & 0x3f));
            }
        }
    }

    public async Task WriteBytesEscaped(byte[] bytes, int len, bool noBackslashEscapes)
    {
        if (len * 2 > Buf.Length - _pos)
        {
            // makes buf bigger (up to 16M)
            if (Buf.Length != _maxPacketLength) await GrowBuffer(len * 2);

            // data may be bigger than buf.
            // must flush buf when full (and reset position to 0)
            if (len * 2 > Buf.Length - _pos)
            {
                if (_mark != -1)
                {
                    await GrowBuffer(len * 2);
                    if (_mark != -1) await FlushBufferStopAtMark();
                }
                else
                {
                    // not enough space in buf, will fill buf
                    if (Buf.Length <= _pos) await WriteSocket(false);
                    if (noBackslashEscapes)
                        for (var i = 0; i < len; i++)
                        {
                            if (QUOTE == bytes[i])
                            {
                                Buf[_pos++] = QUOTE;
                                if (Buf.Length <= _pos) await WriteSocket(false);
                            }

                            Buf[_pos++] = bytes[i];
                            if (Buf.Length <= _pos) await WriteSocket(false);
                        }
                    else
                        for (var i = 0; i < len; i++)
                        {
                            if (bytes[i] == QUOTE
                                || bytes[i] == BACKSLASH
                                || bytes[i] == DBL_QUOTE
                                || bytes[i] == ZERO_BYTE)
                            {
                                Buf[_pos++] = Convert.ToByte('\\');
                                if (Buf.Length <= _pos) await WriteSocket(false);
                            }

                            Buf[_pos++] = bytes[i];
                            if (Buf.Length <= _pos) await WriteSocket(false);
                        }

                    return;
                }
            }
        }

        // sure to have enough place filling buf directly
        if (noBackslashEscapes)
            for (var i = 0; i < len; i++)
            {
                if (QUOTE == bytes[i]) Buf[_pos++] = QUOTE;
                Buf[_pos++] = bytes[i];
            }
        else
            for (var i = 0; i < len; i++)
            {
                if (bytes[i] == QUOTE
                    || bytes[i] == BACKSLASH
                    || bytes[i] == '"'
                    || bytes[i] == ZERO_BYTE)
                    Buf[_pos++] = BACKSLASH; // add escape slash
                Buf[_pos++] = bytes[i];
            }
    }

    public async Task WriteEmptyPacket()
    {
        Buf[0] = 0x00;
        Buf[1] = 0x00;
        Buf[2] = 0x00;
        Buf[3] = _sequence.incrementAndGet();
        await _out.WriteAsync(Buf, 0, 4, _cancellationToken);

        if (logger.isTraceEnabled())
            logger.trace(
                $"send com : content length=0 {_serverThreadLog}\n{LoggerHelper.Hex(Buf, 0, 4)}");
        //_out.Flush();
        _cmdLength = 0;
    }

    public async Task Flush()
    {
        await WriteSocket(true);

        // if buf is big, and last query doesn't use at least half of it, resize buf to default
        // value
        if (Buf.Length > SMALL_BUFFER_SIZE && _cmdLength * 2 < Buf.Length) Buf = new byte[SMALL_BUFFER_SIZE];

        _pos = 4;
        _cmdLength = 0;
        _mark = -1;
    }

    public async Task FlushPipeline()
    {
        await WriteSocket(false);

        // if buf is big, and last query doesn't use at least half of it, resize buf to default
        // value
        if (Buf.Length > SMALL_BUFFER_SIZE && _cmdLength * 2 < Buf.Length) Buf = new byte[SMALL_BUFFER_SIZE];

        _pos = 4;
        _cmdLength = 0;
        _mark = -1;
    }

    public bool ThrowMaxAllowedLength(int length)
    {
        if (_maxAllowedPacket != null) return _cmdLength + length >= _maxAllowedPacket;
        return false;
    }

    public void PermitTrace(bool permitTrace)
    {
        _permitTrace = permitTrace;
    }

    public void SetServerThreadId(long? serverThreadId, HostAddress hostAddress)
    {
        var isMaster = hostAddress?.Primary;
        _serverThreadLog =
            "conn="
            + (serverThreadId == null ? "-1" : serverThreadId)
            + (isMaster != null ? " (" + (isMaster.Value ? "M" : "S") + ")" : "");
    }

    public void Mark()
    {
        _mark = _pos;
    }

    public bool IsMarked()
    {
        return _mark != -1;
    }

    public bool HasFlushed()
    {
        return _sequence.Value != -1;
    }

    public async Task FlushBufferStopAtMark()
    {
        var end = _pos;
        _pos = _mark;
        await WriteSocket(true);
        //_out.Flush();
        InitPacket(_cancellationToken);

        Array.Copy(Buf, _mark, Buf, _pos, end - _mark);
        _pos += end - _mark;
        _mark = -1;
        _bufContainDataAfterMark = true;
    }

    public bool BufIsDataAfterMark()
    {
        return _bufContainDataAfterMark;
    }

    public byte[] ResetMark()
    {
        _pos = _mark;
        _mark = -1;

        if (_bufContainDataAfterMark)
        {
            var data = new byte[_pos - 4];
            Array.Copy(Buf, _pos, data, 0, _pos - 4);
            InitPacket(_cancellationToken);
            _bufContainDataAfterMark = false;
            return data;
        }

        return null;
    }

    public void InitPacket(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _sequence.Value = 0xff;
        _compressSequence.Value = 0xff;
        _pos = 4;
        _cmdLength = 0;
    }

    public void Close()
    {
        _out.Close();
    }

    private async Task GrowBuffer(int len)
    {
        var bufLength = Buf.Length;
        int newCapacity;
        if (bufLength == SMALL_BUFFER_SIZE)
        {
            if (len + _pos <= MEDIUM_BUFFER_SIZE)
                newCapacity = MEDIUM_BUFFER_SIZE;
            else if (len + _pos <= LARGE_BUFFER_SIZE)
                newCapacity = LARGE_BUFFER_SIZE;
            else
                newCapacity = _maxPacketLength;
        }
        else if (bufLength == MEDIUM_BUFFER_SIZE)
        {
            if (len + _pos < LARGE_BUFFER_SIZE)
                newCapacity = LARGE_BUFFER_SIZE;
            else
                newCapacity = _maxPacketLength;
        }
        else if (_bufContainDataAfterMark)
        {
            // want to add some information to buf without having the command Header
            // must grow buf until having all the query
            newCapacity = Math.Max(len + _pos, _maxPacketLength);
        }
        else
        {
            newCapacity = _maxPacketLength;
        }

        if (len + _pos > newCapacity)
            if (_mark != -1)
            {
                // buf is > 16M with mark.
                // flush until mark, reset pos at beginning
                await FlushBufferStopAtMark();

                if (len + _pos <= bufLength) return;

                // need to keep all data, buf can grow more than _maxPacketLength
                // grow buf if needed
                if (bufLength == _maxPacketLength) return;
                if (len + _pos > newCapacity) newCapacity = Math.Min(_maxPacketLength, len + _pos);
            }

        var newBuf = new byte[newCapacity];
        Array.Copy(Buf, 0, newBuf, 0, _pos);
        Buf = newBuf;
    }

    private void CheckMaxAllowedLength(int length)
    {
        if (_maxAllowedPacket != null)
            if (_cmdLength + length >= _maxAllowedPacket)
                // launch exception only if no packet has been sent.
                throw new DbMaxAllowedPacketException(
                    "query size ("
                    + (_cmdLength + length)
                    + ") is >= to max_allowed_packet ("
                    + _maxAllowedPacket
                    + ")",
                    _cmdLength != 0);
    }

    private async Task WriteSocket(bool commandEnd)
    {
        if (_pos > 4)
        {
            Buf[0] = (byte)(_pos - 4);
            Buf[1] = (byte)((_pos - 4) >>> 8);
            Buf[2] = (byte)((_pos - 4) >>> 16);
            Buf[3] = _sequence.incrementAndGet();
            CheckMaxAllowedLength(_pos - 4);
            await _out.WriteAsync(Buf, 0, _pos, _cancellationToken);
            // if (commandEnd) _out.Flush();
            _cmdLength += _pos - 4;

            if (logger.isTraceEnabled())
            {
                if (_permitTrace)
                    logger.trace(
                        $"send: {_serverThreadLog}\n{LoggerHelper.Hex(Buf, 0, _pos, _maxQuerySizeToLog)}");
                else
                    logger.trace($"send: content length={_pos - 4} {_serverThreadLog} com=<hidden>");
            }

            // if last com fill the max size, must send an empty com to indicate command end.
            if (commandEnd && _pos == _maxPacketLength) WriteEmptyPacket();

            _pos = 4;
        }
    }
}