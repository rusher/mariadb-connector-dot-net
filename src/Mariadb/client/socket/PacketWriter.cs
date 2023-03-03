using System.Net.Sockets;
using System.Text;
using Mariadb.client.util;
using Mariadb.utils.exception;
using Mariadb.utils.log;

namespace Mariadb.client.socket;

public class PacketWriter : IWriter
{
    
  /** initial buffer size */
  public static int SMALL_BUFFER_SIZE = 8192;

  private static readonly Ilogger logger = Loggers.getLogger("PacketWriter");
  private static readonly byte QUOTE = (byte) '\'';
  private static readonly byte DBL_QUOTE = (byte) '"';
  private static readonly byte ZERO_BYTE = (byte) '\0';
  private static readonly byte BACKSLASH = (byte) '\\';
  private static readonly int MEDIUM_BUFFER_SIZE = 128 * 1024;
  private static readonly int LARGE_BUFFER_SIZE = 1024 * 1024;
  private static readonly int MAX_PACKET_LENGTH = 0x00ffffff + 4;
  private readonly uint _maxQuerySizeToLog;
  private readonly NetworkStream _out;
  private int _maxPacketLength = MAX_PACKET_LENGTH;
  private int? _maxAllowedPacket;
  private long _cmdLength;
  private bool _permitTrace = true;
  private string _serverThreadLog = "";
  private int _mark = -1;
  private bool _bufContainDataAfterMark = false;

  private byte[] _buf;
  private int _pos = 4;
  private MutableByte _sequence;
  protected MutableByte _compressSequence;

  public PacketWriter(
    NetworkStream outStream,
      uint maxQuerySizeToLog,
      int? maxAllowedPacket,
      MutableByte sequence,
      MutableByte compressSequence) {
    _out = outStream;
    _buf = new byte[SMALL_BUFFER_SIZE];
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
      if (value > _buf.Length) GrowBuffer(value);
      _pos = value;
    }
  }
  public byte[] Buf
  {
    get => _buf;
  }
  
  public long GetCmdLength() {
    return _cmdLength;
  }

  public void WriteByte(int value) {
    if (_pos >= _buf.Length) {
      if (_pos >= _maxPacketLength && !_bufContainDataAfterMark) {
        // buf is more than a Packet, must flushbuf()
        WriteSocket(false);
      } else {
        GrowBuffer(1);
      }
    }
    _buf[_pos++] = (byte) value;
  }

  public void WriteShort(short value) {
    if (2 > _buf.Length - _pos) {
      // not enough space remaining
      WriteBytes(new byte[] {(byte) value, (byte) (value >> 8)}, 0, 2);
      return;
    }

    _buf[_pos] = (byte) value;
    _buf[_pos + 1] = (byte) (value >> 8);
    _pos += 2;
  }

  public void WriteInt(int value) {
    if (4 > _buf.Length - Pos) {
      // not enough space remaining
      byte[] arr = new byte[4];
      arr[0] = (byte) value;
      arr[1] = (byte) (value >> 8);
      arr[2] = (byte) (value >> 16);
      arr[3] = (byte) (value >> 24);
      WriteBytes(arr, 0, 4);
      return;
    }

    _buf[_pos] = (byte) value;
    _buf[_pos + 1] = (byte) (value >> 8);
    _buf[_pos + 2] = (byte) (value >> 16);
    _buf[_pos + 3] = (byte) (value >> 24);
    _pos += 4;
  }

  public void WriteLong(long value) {
    if (8 > _buf.Length - _pos) {
      // not enough space remaining
      byte[] arr = new byte[8];
      arr[0] = (byte) value;
      arr[1] = (byte) (value >> 8);
      arr[2] = (byte) (value >> 16);
      arr[3] = (byte) (value >> 24);
      arr[4] = (byte) (value >> 32);
      arr[5] = (byte) (value >> 40);
      arr[6] = (byte) (value >> 48);
      arr[7] = (byte) (value >> 56);
      WriteBytes(arr, 0, 8);
      return;
    }

    _buf[_pos] = (byte) value;
    _buf[_pos + 1] = (byte) (value >> 8);
    _buf[_pos + 2] = (byte) (value >> 16);
    _buf[_pos + 3] = (byte) (value >> 24);
    _buf[_pos + 4] = (byte) (value >> 32);
    _buf[_pos + 5] = (byte) (value >> 40);
    _buf[_pos + 6] = (byte) (value >> 48);
    _buf[_pos + 7] = (byte) (value >> 56);
    _pos += 8;
  }

  public void WriteDouble(double value) {
    WriteBytes(BitConverter.GetBytes(value), 0, 8);
  }

  public void WriteFloat(float value) {
    WriteBytes(BitConverter.GetBytes(value), 0, 4);
  }

  public void WriteBytes(byte[] arr) {
    WriteBytes(arr, 0, arr.Length);
  }

  public void WriteBytesAtPos(byte[] arr, int pos) {
    Array.Copy(arr, 0, _buf, pos, arr.Length);
  }

  public void WriteBytes(byte[] arr, int off, int len) {
    if (len > _buf.Length - _pos) {
      if (_buf.Length != _maxPacketLength) {
        GrowBuffer(len);
      }

      // max buf size
      if (len > _buf.Length - _pos) {

        if (_mark != -1) {
          GrowBuffer(len);
          if (_mark != -1) {
            FlushBufferStopAtMark();
          }
        }

        if (len > _buf.Length - _pos) {
          // not enough space in buf, will stream :
          // fill buf and flush until all data are snd
          int remainingLen = len;
          do {
            int lenToFillbuf = Math.Min(_maxPacketLength - _pos, remainingLen);
            Array.Copy(arr, off, _buf, _pos, lenToFillbuf);
            remainingLen -= lenToFillbuf;
            off += lenToFillbuf;
            _pos += lenToFillbuf;
            if (remainingLen > 0) {
              WriteSocket(false);
            } else {
              break;
            }
          } while (true);
          return;
        }
      }
    }

    Array.Copy(arr, off, _buf, _pos, len);
    _pos += len;
  }

  public void WriteLength(long length) {
    if (length < 251) {
      WriteByte((byte) length);
      return;
    }

    if (length < 65536) {

      if (3 > _buf.Length - _pos) {
        // not enough space remaining
        byte[] arr = new byte[3];
        arr[0] = (byte) 0xfc;
        arr[1] = (byte) length;
        arr[2] = (byte) (length >>> 8);
        WriteBytes(arr, 0, 3);
        return;
      }

      _buf[_pos] = (byte) 0xfc;
      _buf[_pos + 1] = (byte) length;
      _buf[_pos + 2] = (byte) (length >>> 8);
      _pos += 3;
      return;
    }

    if (length < 16777216) {

      if (4 > _buf.Length - _pos) {
        // not enough space remaining
        byte[] arr = new byte[4];
        arr[0] = (byte) 0xfd;
        arr[1] = (byte) length;
        arr[2] = (byte) (length >>> 8);
        arr[3] = (byte) (length >>> 16);
        WriteBytes(arr, 0, 4);
        return;
      }

      _buf[_pos] = (byte) 0xfd;
      _buf[_pos + 1] = (byte) length;
      _buf[_pos + 2] = (byte) (length >>> 8);
      _buf[_pos + 3] = (byte) (length >>> 16);
      _pos += 4;
      return;
    }

    if (9 > _buf.Length - _pos) {
      // not enough space remaining
      byte[] arr = new byte[9];
      arr[0] = (byte) 0xfe;
      arr[1] = (byte) length;
      arr[2] = (byte) (length >>> 8);
      arr[3] = (byte) (length >>> 16);
      arr[4] = (byte) (length >>> 24);
      arr[5] = (byte) (length >>> 32);
      arr[6] = (byte) (length >>> 40);
      arr[7] = (byte) (length >>> 48);
      arr[8] = (byte) (length >>> 56);
      WriteBytes(arr, 0, 9);
      return;
    }

    _buf[_pos] = (byte) 0xfe;
    _buf[_pos + 1] = (byte) length;
    _buf[_pos + 2] = (byte) (length >>> 8);
    _buf[_pos + 3] = (byte) (length >>> 16);
    _buf[_pos + 4] = (byte) (length >>> 24);
    _buf[_pos + 5] = (byte) (length >>> 32);
    _buf[_pos + 6] = (byte) (length >>> 40);
    _buf[_pos + 7] = (byte) (length >>> 48);
    _buf[_pos + 8] = (byte) (length >>> 56);
    _pos += 9;
  }

  public void WriteAscii(string str) {
    int len = str.Length;
    if (len > _buf.Length - _pos) {
      byte[] arr = Encoding.ASCII.GetBytes(str);
      WriteBytes(arr, 0, arr.Length);
      return;
    }
    for (int off = 0; off < len; ) {
      _buf[_pos++] = (byte) str[off++];
    }
  }

  public void WriteString(string str) {
    int charsLength = str.Length;

    // not enough space remaining
    if (charsLength * 3 >= _buf.Length - _pos) {
      byte[] arr = Encoding.UTF8.GetBytes(str);
      WriteBytes(arr, 0, arr.Length);
      return;
    }

    // create UTF-8 byte array
    // since java char are internally using UTF-16 using surrogate's pattern, 4 bytes unicode
    // characters will
    // represent 2 characters : example "\uD83C\uDFA4" = 🎤 unicode 8 "no microphones"
    // so max size is 3 * charLength
    // (escape characters are 1 byte encoded, so length might only be 2 when escape)
    // + 2 for the quotes for text protocol
    int charsOffset = 0;
    char currChar;

    // quick loop if only ASCII chars for faster escape
    for (;
        charsOffset < charsLength && (currChar = str[charsOffset]) < 0x80;
        charsOffset++) {
      _buf[_pos++] = (byte) currChar;
    }

    // if quick loop not finished
    while (charsOffset < charsLength) {
      currChar = str[charsOffset++];
      if (currChar < 0x80) {
        _buf[_pos++] = (byte) currChar;
      } else if (currChar < 0x800) {
        _buf[_pos++] = (byte) (0xc0 | (currChar >> 6));
        _buf[_pos++] = (byte) (0x80 | (currChar & 0x3f));
      } else if (currChar >= 0xD800 && currChar < 0xE000) {
        // reserved for surrogate - see https://en.wikipedia.org/wiki/UTF-16
        if (currChar < 0xDC00) {
          // is high surrogate
          if (charsOffset + 1 > charsLength) {
            _buf[_pos++] = (byte) 0x63;
          } else {
            char nextChar = str[charsOffset];
            if (nextChar >= 0xDC00 && nextChar < 0xE000) {
              // is low surrogate
              int surrogatePairs =
                  ((currChar << 10) + nextChar) + (0x010000 - (0xD800 << 10) - 0xDC00);
              _buf[_pos++] = (byte) (0xf0 | ((surrogatePairs >> 18)));
              _buf[_pos++] = (byte) (0x80 | ((surrogatePairs >> 12) & 0x3f));
              _buf[_pos++] = (byte) (0x80 | ((surrogatePairs >> 6) & 0x3f));
              _buf[_pos++] = (byte) (0x80 | (surrogatePairs & 0x3f));
              charsOffset++;
            } else {
              // must have low surrogate
              _buf[_pos++] = (byte) 0x3f;
            }
          }
        } else {
          // low surrogate without high surrogate before
          _buf[_pos++] = (byte) 0x3f;
        }
      } else {
        _buf[_pos++] = (byte) (0xe0 | ((currChar >> 12)));
        _buf[_pos++] = (byte) (0x80 | ((currChar >> 6) & 0x3f));
        _buf[_pos++] = (byte) (0x80 | (currChar & 0x3f));
      }
    }
  }

  public void WriteStringEscaped(string str, bool noBackslashEscapes) {

    int charsLength = str.Length;

    // not enough space remaining
    if (charsLength * 3 >= _buf.Length - _pos) {
      byte[] arr = Encoding.UTF8.GetBytes(str);
      WriteBytesEscaped(arr, arr.Length, noBackslashEscapes);
      return;
    }

    // create UTF-8 byte array
    // since java char are internally using UTF-16 using surrogate's pattern, 4 bytes unicode
    // characters will
    // represent 2 characters : example "\uD83C\uDFA4" = 🎤 unicode 8 "no microphones"
    // so max size is 3 * charLength
    // (escape characters are 1 byte encoded, so length might only be 2 when escape)
    // + 2 for the quotes for text protocol
    int charsOffset = 0;
    char currChar;

    // quick loop if only ASCII chars for faster escape
    if (noBackslashEscapes) {
      for (;
          charsOffset < charsLength && (currChar = str[charsOffset]) < 0x80;
          charsOffset++) {
        if (currChar == QUOTE) {
          _buf[_pos++] = QUOTE;
        }
        _buf[_pos++] = (byte) currChar;
      }
    } else {
      for (;
          charsOffset < charsLength && (currChar = str[charsOffset]) < 0x80;
          charsOffset++) {
        if (currChar == BACKSLASH || currChar == QUOTE || currChar == 0 || currChar == DBL_QUOTE) {
          _buf[_pos++] = BACKSLASH;
        }
        _buf[_pos++] = (byte) currChar;
      }
    }

    // if quick loop not finished
    while (charsOffset < charsLength) {
      currChar = str[charsOffset++];
      if (currChar < 0x80) {
        if (noBackslashEscapes) {
          if (currChar == QUOTE) {
            _buf[_pos++] = QUOTE;
          }
        } else if (currChar == BACKSLASH
            || currChar == QUOTE
            || currChar == ZERO_BYTE
            || currChar == DBL_QUOTE) {
          _buf[_pos++] = BACKSLASH;
        }
        _buf[_pos++] = (byte) currChar;
      } else if (currChar < 0x800) {
        _buf[_pos++] = (byte) (0xc0 | (currChar >> 6));
        _buf[_pos++] = (byte) (0x80 | (currChar & 0x3f));
      } else if (currChar >= 0xD800 && currChar < 0xE000) {
        // reserved for surrogate - see https://en.wikipedia.org/wiki/UTF-16
        if (currChar < 0xDC00) {
          // is high surrogate
          if (charsOffset + 1 > charsLength) {
            _buf[_pos++] = (byte) 0x63;
          } else {
            char nextChar = str[charsOffset];
            if (nextChar >= 0xDC00 && nextChar < 0xE000) {
              // is low surrogate
              int surrogatePairs =
                  ((currChar << 10) + nextChar) + (0x010000 - (0xD800 << 10) - 0xDC00);
              _buf[_pos++] = (byte) (0xf0 | ((surrogatePairs >> 18)));
              _buf[_pos++] = (byte) (0x80 | ((surrogatePairs >> 12) & 0x3f));
              _buf[_pos++] = (byte) (0x80 | ((surrogatePairs >> 6) & 0x3f));
              _buf[_pos++] = (byte) (0x80 | (surrogatePairs & 0x3f));
              charsOffset++;
            } else {
              // must have low surrogate
              _buf[_pos++] = (byte) 0x3f;
            }
          }
        } else {
          // low surrogate without high surrogate before
          _buf[_pos++] = (byte) 0x3f;
        }
      } else {
        _buf[_pos++] = (byte) (0xe0 | ((currChar >> 12)));
        _buf[_pos++] = (byte) (0x80 | ((currChar >> 6) & 0x3f));
        _buf[_pos++] = (byte) (0x80 | (currChar & 0x3f));
      }
    }
  }

  public void WriteBytesEscaped(byte[] bytes, int len, bool noBackslashEscapes) 
  {
    if (len * 2 > _buf.Length - _pos) {

      // makes buf bigger (up to 16M)
      if (_buf.Length != _maxPacketLength) {
        GrowBuffer(len * 2);
      }

      // data may be bigger than buf.
      // must flush buf when full (and reset position to 0)
      if (len * 2 > _buf.Length - _pos) {

        if (_mark != -1) {
          GrowBuffer(len * 2);
          if (_mark != -1) {
            FlushBufferStopAtMark();
          }

        } else {
          // not enough space in buf, will fill buf
          if (_buf.Length <= _pos) {
            WriteSocket(false);
          }
          if (noBackslashEscapes) {
            for (int i = 0; i < len; i++) {
              if (QUOTE == bytes[i]) {
                _buf[_pos++] = QUOTE;
                if (_buf.Length <= _pos) {
                  WriteSocket(false);
                }
              }
              _buf[_pos++] = bytes[i];
              if (_buf.Length <= _pos) {
                WriteSocket(false);
              }
            }
          } else {
            for (int i = 0; i < len; i++) {
              if (bytes[i] == QUOTE
                  || bytes[i] == BACKSLASH
                  || bytes[i] == DBL_QUOTE
                  || bytes[i] == ZERO_BYTE) {
                _buf[_pos++] = Convert.ToByte('\\');
                if (_buf.Length <= _pos) {
                  WriteSocket(false);
                }
              }
              _buf[_pos++] = bytes[i];
              if (_buf.Length <= _pos) {
                WriteSocket(false);
              }
            }
          }
          return;
        }
      }
    }

    // sure to have enough place filling buf directly
    if (noBackslashEscapes) {
      for (int i = 0; i < len; i++) {
        if (QUOTE == bytes[i]) {
          _buf[_pos++] = QUOTE;
        }
        _buf[_pos++] = bytes[i];
      }
    } else {
      for (int i = 0; i < len; i++) {
        if (bytes[i] == QUOTE
            || bytes[i] == BACKSLASH
            || bytes[i] == '"'
            || bytes[i] == ZERO_BYTE) {
          _buf[_pos++] = BACKSLASH; // add escape slash
        }
        _buf[_pos++] = bytes[i];
      }
    }
  }

  private void GrowBuffer(int len) {
    int bufLength = _buf.Length;
    int newCapacity;
    if (bufLength == SMALL_BUFFER_SIZE) {
      if (len + _pos <= MEDIUM_BUFFER_SIZE) {
        newCapacity = MEDIUM_BUFFER_SIZE;
      } else if (len + _pos <= LARGE_BUFFER_SIZE) {
        newCapacity = LARGE_BUFFER_SIZE;
      } else {
        newCapacity = _maxPacketLength;
      }
    } else if (bufLength == MEDIUM_BUFFER_SIZE) {
      if (len + _pos < LARGE_BUFFER_SIZE) {
        newCapacity = LARGE_BUFFER_SIZE;
      } else {
        newCapacity = _maxPacketLength;
      }
    } else if (_bufContainDataAfterMark) {
      // want to add some information to buf without having the command Header
      // must grow buf until having all the query
      newCapacity = Math.Max(len + _pos, _maxPacketLength);
    } else {
      newCapacity = _maxPacketLength;
    }

    if (len + _pos > newCapacity) {
      if (_mark != -1) {
        // buf is > 16M with mark.
        // flush until mark, reset pos at beginning
        FlushBufferStopAtMark();

        if (len + _pos <= bufLength) {
          return;
        }

        // need to keep all data, buf can grow more than _maxPacketLength
        // grow buf if needed
        if (bufLength == _maxPacketLength) return;
        if (len + _pos > newCapacity) {
          newCapacity = Math.Min(_maxPacketLength, len + _pos);
        }
      }
    }

    byte[] newBuf = new byte[newCapacity];
    Array.Copy(_buf, 0, newBuf, 0, _pos);
    _buf = newBuf;
  }

  public void WriteEmptyPacket() {

    _buf[0] = (byte) 0x00;
    _buf[1] = (byte) 0x00;
    _buf[2] = (byte) 0x00;
    _buf[3] = _sequence.incrementAndGet();
    _out.Write(_buf, 0, 4);

    if (logger.isTraceEnabled()) {
      logger.trace(
          $"send com : content length=0 {_serverThreadLog}\n{LoggerHelper.Hex(_buf, 0, 4)}");
    }
    _out.Flush();
    _cmdLength = 0;
  }

  public void Flush() {
    WriteSocket(true);

    // if buf is big, and last query doesn't use at least half of it, resize buf to default
    // value
    if (_buf.Length > SMALL_BUFFER_SIZE && _cmdLength * 2 < _buf.Length) {
      _buf = new byte[SMALL_BUFFER_SIZE];
    }

    _pos = 4;
    _cmdLength = 0;
    _mark = -1;
  }

  public void FlushPipeline() {
    WriteSocket(false);

    // if buf is big, and last query doesn't use at least half of it, resize buf to default
    // value
    if (_buf.Length > SMALL_BUFFER_SIZE && _cmdLength * 2 < _buf.Length) {
      _buf = new byte[SMALL_BUFFER_SIZE];
    }

    _pos = 4;
    _cmdLength = 0;
    _mark = -1;
  }

  private void CheckMaxAllowedLength(int length) {
    if (_maxAllowedPacket != null) {
      if (_cmdLength + length >= _maxAllowedPacket) {
        // launch exception only if no packet has been sent.
        throw new DbMaxAllowedPacketException(
            "query size ("
                + (_cmdLength + length)
                + ") is >= to max_allowed_packet ("
                + _maxAllowedPacket
                + ")",
            _cmdLength != 0);
      }
    }
  }

  public bool ThrowMaxAllowedLength(int length) {
    if (_maxAllowedPacket != null) return _cmdLength + length >= _maxAllowedPacket;
    return false;
  }

  public void PermitTrace(bool permitTrace) {
    _permitTrace = permitTrace;
  }

  public void SetServerThreadId(long serverThreadId, HostAddress hostAddress) {
    bool? isMaster = hostAddress?.Primary;
    _serverThreadLog =
        "conn="
            + (serverThreadId == null ? "-1" : serverThreadId)
            + ((isMaster != null) ? " (" + (isMaster.Value ? "M" : "S") + ")" : "");
  }

  public void Mark() {
    _mark = _pos;
  }

  public bool IsMarked() {
    return _mark != -1;
  }

  public bool HasFlushed() {
    return _sequence.Value != -1;
  }

  public void FlushBufferStopAtMark() {
    int end = _pos;
    _pos = _mark;
    WriteSocket(true);
    _out.Flush();
    InitPacket();

    Array.Copy(_buf, _mark, _buf, _pos, end - _mark);
    _pos += end - _mark;
    _mark = -1;
    _bufContainDataAfterMark = true;
  }

  public bool BufIsDataAfterMark() {
    return _bufContainDataAfterMark;
  }

  public byte[] ResetMark() {
    _pos = _mark;
    _mark = -1;

    if (_bufContainDataAfterMark)
    {
      byte[] data = new byte[_pos - 4];
      Array.Copy(_buf, _pos, data, 0, _pos - 4);
      InitPacket();
      _bufContainDataAfterMark = false;
      return data;
    }
    return null;
  }

  public void InitPacket() {
    _sequence.Value = 0xff;
    _compressSequence.Value = 0xff;
    _pos = 4;
    _cmdLength = 0;
  }

  private void WriteSocket(bool commandEnd) {
    if (_pos > 4) {
      _buf[0] = (byte) (_pos - 4);
      _buf[1] = (byte) ((_pos - 4) >>> 8);
      _buf[2] = (byte) ((_pos - 4) >>> 16);
      _buf[3] = _sequence.incrementAndGet();
      CheckMaxAllowedLength(_pos - 4);
      _out.Write(_buf, 0, _pos);
      if (commandEnd) _out.Flush();
      _cmdLength += _pos - 4;

      if (logger.isTraceEnabled()) {
        if (_permitTrace) {
          logger.trace(
              $"send: {_serverThreadLog}\n{LoggerHelper.Hex(_buf, 0, _pos, _maxQuerySizeToLog)}");
        } else {
          logger.trace($"send: content length={_pos - 4} {_serverThreadLog} com=<hidden>");
        }
      }

      // if last com fill the max size, must send an empty com to indicate command end.
      if (commandEnd && _pos == _maxPacketLength) {
        WriteEmptyPacket();
      }

      _pos = 4;
    }
  }

  public void Close() {
    _out.Close();
  }
}