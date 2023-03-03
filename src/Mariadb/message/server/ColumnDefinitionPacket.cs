using Mariadb.client;
using Mariadb.client.util;
using Mariadb.utils;
using Mariadb.utils.constant;

namespace Mariadb.message.server;

public class ColumnDefinitionPacket: IColumn, IServerMessage
{
    
  private readonly IReadableByteBuf _buf;
  protected readonly int _charset;
  protected readonly  long _columnLength;
  protected readonly DataType _dataType;
  protected readonly byte _decimals;
  private readonly int _flags;
  private readonly int[] _stringPos;
  protected readonly string? _extTypeName;
  protected readonly string? _extTypeFormat;

  public ColumnDefinitionPacket(
    IReadableByteBuf buf,
      int charset,
      long columnLength,
      DataType dataType,
      byte decimals,
      int flags,
      int[] stringPos,
      string? extTypeName,
      string? extTypeFormat) {
    _buf = buf;
    _charset = charset;
    _columnLength = columnLength;
    _dataType = dataType;
    _decimals = decimals;
    _flags = flags;
    _stringPos = stringPos;
    _extTypeName = extTypeName;
    _extTypeFormat = extTypeFormat;
  }

  public string GetSchema() {
    _buf.SetPos(_stringPos[0]);
    return _buf.ReadString(_buf.ReadIntLengthEncodedNotNull());
  }

  public String GetTable() {
    _buf.SetPos(_stringPos[1]);
    return _buf.ReadString(_buf.ReadIntLengthEncodedNotNull());
  }

  public string GetTableAlias() {
    _buf.SetPos(_stringPos[2]);
    return _buf.ReadString(_buf.ReadIntLengthEncodedNotNull());
  }

  public String GetColumnName() {
    _buf.SetPos(_stringPos[3]);
    return _buf.ReadString(_buf.ReadIntLengthEncodedNotNull());
  }

  public String GetColumnAlias() {
    _buf.SetPos(_stringPos[4]);
    return _buf.ReadString(_buf.ReadIntLengthEncodedNotNull());
  }
  
  public virtual int GetPrecision() {
    return (int) GetColumnLength();
  }
  
  public long GetColumnLength() {
    return _columnLength;
  }

  public DataType GetType() {
    return _dataType;
  }

  public byte GetDecimals() {
    return _decimals;
  }

  public bool IsSigned() {
    return (_flags & ColumnFlags.UNSIGNED) == 0;
  }

  public int GetDisplaySize() {
    if (!IsBinary()
        && (_dataType == DataType.VARCHAR
            || _dataType == DataType.JSON
            || _dataType == DataType.ENUM
            || _dataType == DataType.SET
            || _dataType == DataType.VARSTRING
            || _dataType == DataType.STRING
            || _dataType == DataType.BLOB
            || _dataType == DataType.TINYBLOB
            || _dataType == DataType.MEDIUMBLOB
            || _dataType == DataType.LONGBLOB)) {
      int? maxWidth = CharsetEncodingLength.MaxCharlen[_charset];
      if (maxWidth != null) return (int) (_columnLength / maxWidth.Value);
    }
    return (int) _columnLength;
  }

  public bool IsPrimaryKey() {
    return (_flags & ColumnFlags.PRIMARY_KEY) > 0;
  }

  public bool IsAutoIncrement() {
    return (_flags & ColumnFlags.AUTO_INCREMENT) > 0;
  }

  public bool HasDefault() {
    return (_flags & ColumnFlags.NO_DEFAULT_VALUE_FLAG) == 0;
  }

  // doesn't use & 128 bit filter, because char binary and varchar binary are not binary (handle
  // like string), but have the binary flag
  public bool IsBinary() {
    return _charset == 63;
  }

  public int GetFlags() {
    return _flags;
  }

  public string GetExtTypeName() {
    return _extTypeName;
  }

}