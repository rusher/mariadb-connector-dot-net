using Mariadb.client.util;
using Mariadb.message.server;
using Mariadb.utils.exception;

namespace Mariadb.client.decoder;

public class SignedTinyIntColumn : ColumnDefinitionPacket, IColumnDecoder
{
    
  public SignedTinyIntColumn(
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
    if (_columnLength == 1) {
      return DecodeBooleanText(buf, length);
    }
    return (int) buf.Atoll(length);
  }

  public Object GetDefaultBinary(Configuration conf, IReadableByteBuf buf, int length) {
    if (_columnLength == 1) {
      return DecodeBooleanBinary(buf, length);
    }
    if (IsSigned()) {
      return (int) buf.ReadByte();
    }
    return (int) buf.ReadUnsignedByte();
  }

  public bool DecodeBooleanText(IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length) != "0";
  }

  public bool DecodeBooleanBinary(IReadableByteBuf buf, int length) {
    return buf.ReadByte() != 0;
  }

  public byte DecodeByteText(IReadableByteBuf buf, int length)  {
    long result = buf.Atoll(length);
    if ((byte) result != result) {
      throw new ArgumentException("byte overflow");
    }
    return (byte) result;
  }

  public byte DecodeByteBinary(IReadableByteBuf buf, int length) {
    if (IsSigned()) return buf.ReadByte();
    long result = buf.ReadUnsignedByte();

    if ((byte) result != result) {
      throw new ArgumentException("byte overflow");
    }
    return (byte) result;
  }

  public string DecodeStringText(IReadableByteBuf buf, int length) {
    return buf.ReadAscii(length);
  }

  public string? DecodeStringBinary(IReadableByteBuf buf, int length) {
    if (!IsSigned()) {
      return buf.ReadUnsignedByte().ToString();
    }
    return buf.ReadByte().ToString();
  }

  public short DecodeShortText(IReadableByteBuf buf, int length) {
    return (short) buf.Atoll(length);
  }

  public short DecodeShortBinary(IReadableByteBuf buf, int length) {
    return (IsSigned() ? buf.ReadByte() : buf.ReadUnsignedByte());
  }

  public int DecodeIntText(IReadableByteBuf buf, int length) {
    return (int) buf.Atoll(length);
  }

  public int DecodeIntBinary(IReadableByteBuf buf, int length) {
    return (IsSigned() ? buf.ReadByte() : buf.ReadUnsignedByte());
  }

  public long DecodeLongText(IReadableByteBuf buf, int length) {
    return buf.Atoll(length);
  }

  public long DecodeLongBinary(IReadableByteBuf buf, int length) {
    if (!IsSigned()) {
      return buf.ReadUnsignedByte();
    }
    return buf.ReadByte();
  }

  public float DecodeFloatText(IReadableByteBuf buf, int length) {
    return float.Parse(buf.ReadAscii(length));
  }

  public float DecodeFloatBinary(IReadableByteBuf buf, int length) {
    if (!IsSigned()) {
      return buf.ReadUnsignedByte();
    }
    return buf.ReadByte();
  }

  public double DecodeDoubleText(IReadableByteBuf buf, int length) {
    return double.Parse(buf.ReadAscii(length));
  }

  public double DecodeDoubleBinary(IReadableByteBuf buf, int length) {
    if (!IsSigned()) {
      return buf.ReadUnsignedByte();
    }
    return buf.ReadByte();
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