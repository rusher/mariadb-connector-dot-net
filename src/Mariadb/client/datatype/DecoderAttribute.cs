using Mariadb.client.util;
using Mariadb.message.server;

namespace Mariadb.client;

public class DecoderAttribute : Attribute
{
    public DecoderAttribute(
        Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder> signedDecoder, 
        Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder> unsignedDecoder)
    {
    }
}