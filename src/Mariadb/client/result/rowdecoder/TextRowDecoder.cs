using Mariadb.client.impl;
using Mariadb.client.util;
using Mariadb.message.server;
using Mariadb.plugin;

namespace Mariadb.client.result.rowdecoder;

public class TextRowDecoder : IRowDecoder
{
    
  public T Decode<T>(
    ICodec<T> codec,
      StandardReadableByteBuf rowBuf,
      int fieldLength,
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex) 
  {
    return codec.DecodeText(rowBuf, fieldLength, metadataList[fieldIndex.Value]);
  }

  public Object defaultDecode(
      Configuration conf,
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].GetDefaultText(conf, rowBuf, fieldLength);
  }

  public String DecodeString(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeStringText(rowBuf, fieldLength);
  }

  public byte DecodeByte(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeByteText(rowBuf, fieldLength);
  }

  public bool DecodeBoolean(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeBooleanText(rowBuf, fieldLength);
  }

  public DateTime DecodeDateTime(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeDateTimeText(rowBuf, fieldLength);
  }

  public short DecodeShort(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeShortText(rowBuf, fieldLength);
  }

  public int DecodeInt(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeIntText(rowBuf, fieldLength);
  }

  public long DecodeLong(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeLongText(rowBuf, fieldLength);
  }

  public float DecodeFloat(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeFloatText(rowBuf, fieldLength);
  }

  public double DecodeDouble(
      IColumnDecoder[] metadataList,
      MutableInt fieldIndex,
      StandardReadableByteBuf rowBuf,
      int fieldLength) {
    return metadataList[fieldIndex.Value].DecodeDoubleText(rowBuf, fieldLength);
  }

  public bool WasNull(byte[] nullBitmap, MutableInt fieldIndex, int fieldLength) {
    return fieldLength == MariadbDataReader.NULL_LENGTH;
  }

  public int SetPosition(
      int newIndex,
      MutableInt fieldIndex,
      int maxIndex,
      StandardReadableByteBuf rowBuf,
      byte[] nullBitmap,
      IColumnDecoder[] metadataList) {
    if (fieldIndex.Value >= newIndex) {
      fieldIndex.Value = 0;
      rowBuf.Pos = 0;
    } else {
      fieldIndex.incrementAndGet();
    }

    while (fieldIndex.Value < newIndex) {
      rowBuf.SkipLengthEncoded();
      fieldIndex.incrementAndGet();
    }

    byte len = rowBuf.ReadByte();
    switch (len) {
      case (byte) 251:
        return MariadbDataReader.NULL_LENGTH;
      case (byte) 252:
        return rowBuf.ReadUnsignedShort();
      case (byte) 253:
        return rowBuf.ReadUnsignedMedium();
      case (byte) 254:
        int fieldLength = (int) rowBuf.ReadUnsignedInt();
        rowBuf.Skip(4);
        return fieldLength;
      default:
        return len & 0xff;
    }
  }

}