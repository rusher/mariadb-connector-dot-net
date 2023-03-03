using Mariadb.client.util;
using Mariadb.message.server;
using Mariadb.utils.exception;

namespace Mariadb.client.decoder;

public class GuidColumn : ColumnDefinitionPacket, IColumnDecoder
{
    
  public GuidColumn(
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

  public object GetDefaultText(Configuration conf, IReadableByteBuf buf, int length) {
    return Guid.Parse(buf.ReadAscii(length));
  }

  public Object GetDefaultBinary(Configuration conf, IReadableByteBuf buf, int length) {
    return Guid.Parse(buf.ReadAscii(length));
  }

  public bool DecodeBooleanText(IReadableByteBuf buf, int length) {
    buf.Skip(length);
    throw new ArgumentException("Data type UUID cannot be decoded as bool");
  }

  public bool DecodeBooleanBinary(IReadableByteBuf buf, int length) {
    buf.Skip(length);
    throw new ArgumentException("Data type UUID cannot be decoded as bool");
  }

  public byte DecodeByteText(IReadableByteBuf buf, int length)  {
    buf.Skip(length);
    throw new ArgumentException("Data type UUID cannot be decoded as byte");
  }

  public byte DecodeByteBinary(IReadableByteBuf buf, int length) {
    buf.Skip(length);
    throw new ArgumentException("Data type UUID cannot be decoded as byte");
  }

  public string DecodeStringText(IReadableByteBuf buf, int length)
  {
    return buf.ReadAscii(length);
  }

  public string DecodeStringBinary(IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length);
  }

  public short DecodeShortText(IReadableByteBuf buf, int length) {
    buf.Skip(length);
    throw new ArgumentException("Data type UUID cannot be decoded as short");
  }

  public short DecodeShortBinary(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as short");
  }

  public int DecodeIntText(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as int");  }

  public int DecodeIntBinary(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as int");
  }

  public long DecodeLongText(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as long");
  }

  public long DecodeLongBinary(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as long"); 
  }

  public float DecodeFloatText(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as float");
  }

  public float DecodeFloatBinary(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as float");
  }

  public double DecodeDoubleText(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as double");
  }

  public double DecodeDoubleBinary(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as double");
  }

  public DateTime DecodeDateTimeText(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as DateTime");
  }

  public DateTime DecodeDateTimeBinary(IReadableByteBuf buf, int length) {
    throw new ArgumentException("Data type UUID cannot be decoded as DateTime");
  }

}