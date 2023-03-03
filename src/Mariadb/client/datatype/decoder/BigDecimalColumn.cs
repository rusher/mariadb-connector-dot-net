using Mariadb.client.util;
using Mariadb.message.server;
using Mariadb.utils.exception;

namespace Mariadb.client.decoder;

public class BigDecimalColumn : ColumnDefinitionPacket, IColumnDecoder
{
    
  public BigDecimalColumn(
      IReadableByteBuf buf,
      int charset,
      long length,
      DataType dataType,
      byte decimals,
      int flags,
      int[] stringPos,
      String extTypeName,
      String extTypeFormat) : base(buf, charset, length, dataType, decimals, flags, stringPos, extTypeName, extTypeFormat) {
  }

  public override int GetPrecision() {
    // DECIMAL and OLDDECIMAL are  "exact" fixed-point number.
    // so :
    // - if is signed, 1 byte is saved for sign
    // - if decimal > 0, one byte more for dot
    if (IsSigned()) {
      return (int) (_columnLength - ((_decimals > 0) ? 2 : 1));
    }
    return (int) (_columnLength - ((_decimals > 0) ? 1 : 0));
  }

  public object GetDefaultText(Configuration conf, IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length);
  }

  public Object GetDefaultBinary(Configuration conf, IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length);
  }

  public bool DecodeBooleanText(IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length) != "0";
  }

  public bool DecodeBooleanBinary(IReadableByteBuf buf, int length) {
    return DecodeBooleanText(buf, length);
  }

  public byte DecodeByteText(IReadableByteBuf buf, int length)  {
    string str = buf.ReadAscii(length);
    byte b;
    if (Byte.TryParse(str, out b))
    {
      return b;
    }
    throw new ArgumentException($"DECIMAL value '{str}' cannot be parse as byte value.");
  }

  public byte DecodeByteBinary(IReadableByteBuf buf, int length) {
    return DecodeByteText(buf, length);
  }

  public string DecodeStringText(IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length);
  }

  public string DecodeStringBinary(IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length);
  }

  public short DecodeShortText(IReadableByteBuf buf, int length) {
    string str = buf.ReadAscii(length);
    short b;
    if (short.TryParse(str, out b))
    {
      return b;
    }
    throw new ArgumentException($"DECIMAL value '{str}' cannot be parse as short value.");
  }

  public short DecodeShortBinary(IReadableByteBuf buf, int length) {
    return DecodeShortText(buf, length);
  }

  public int DecodeIntText(IReadableByteBuf buf, int length) {
    string str = buf.ReadAscii(length);
    int b;
    if (int.TryParse(str, out b))
    {
      return b;
    }
    throw new ArgumentException($"DECIMAL value '{str}' cannot be parse as int value.");
  }

  public int DecodeIntBinary(IReadableByteBuf buf, int length) {
    return DecodeIntText(buf, length);
  }

  public long DecodeLongText(IReadableByteBuf buf, int length) {
    string str = buf.ReadAscii(length);
    long b;
    if (long.TryParse(str, out b))
    {
      return b;
    }
    throw new ArgumentException($"DECIMAL value '{str}' cannot be parse as long value.");
  }

  public long DecodeLongBinary(IReadableByteBuf buf, int length) {
    return DecodeLongText(buf, length); 
  }

  public float DecodeFloatText(IReadableByteBuf buf, int length) {
    string str = buf.ReadAscii(length);
    float b;
    if (float.TryParse(str, out b))
    {
      return b;
    }
    throw new ArgumentException($"DECIMAL value '{str}' cannot be parse as float value.");
  }

  public float DecodeFloatBinary(IReadableByteBuf buf, int length) {
    return DecodeFloatText(buf, length); 
  }

  public double DecodeDoubleText(IReadableByteBuf buf, int length) {
    string str = buf.ReadAscii(length);
    double b;
      if (double.TryParse(str, out b))
    {
      return b;
    }
    throw new ArgumentException($"DECIMAL value '{str}' cannot be parse as double value.");
  }

  public double DecodeDoubleBinary(IReadableByteBuf buf, int length) {
    return DecodeDoubleText(buf, length); 
  }

  public DateTime DecodeDateTimeText(IReadableByteBuf buf, int length) {
    buf.Skip(length);
    throw new DbDataException($"Data type {_dataType} cannot be decoded as Date");
  }

  public DateTime DecodeDateTimeBinary(IReadableByteBuf buf, int length) {
    buf.Skip(length);
    throw new DbDataException($"Data type {_dataType} cannot be decoded as Date");
  }

}