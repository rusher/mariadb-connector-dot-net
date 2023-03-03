using Mariadb.client;
using Mariadb.client.socket;
using Mariadb.client.util;
using Mariadb.message.server;

namespace Mariadb.plugin;

public interface ICodec<T>
{
    
  string ClassName();
  bool CanDecode(IColumnDecoder column, Type type);

  bool CanEncode(Object value);

  T DecodeText(
      IReadableByteBuf buffer,
      int length,
      IColumnDecoder column);

  T DecodeBinary(
      IReadableByteBuf buffer,
      int length,
      IColumnDecoder column);

  void EncodeText(IWriter encoder, IContext context, Object value, long? length);

  void EncodeBinary(IWriter encoder, Object value, long? length);

  bool CanEncodeLongData();

  void EncodeLongData(IWriter encoder, T value, long? length);
  byte[] EncodeData(T value, long? length);
  int GetBinaryEncodeType();

}