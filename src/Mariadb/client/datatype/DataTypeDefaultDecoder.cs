using Mariadb.client.decoder;
using Mariadb.client.util;
using Mariadb.message.server;

namespace Mariadb.client;

public class DataTypeDefaultDecoder
{
    
    public static Dictionary<DataType, Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder> > signedDecoders;
    public static Dictionary<DataType, Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder> > unsignedDecoders;

    static DataTypeDefaultDecoder()
    {
        signedDecoders = new Dictionary<DataType, Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder>>();
        signedDecoders.Add(DataType.OLDDECIMAL, BigDecimalLamdba);
        signedDecoders.Add(DataType.TINYINT, SignedTinyIntLamdba);

        unsignedDecoders = new Dictionary<DataType, Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder>>();
        unsignedDecoders.Add(DataType.OLDDECIMAL, BigDecimalLamdba);
    }

    public static Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder> BigDecimalLamdba = (buf,
        charset,
        length,
        dataType,
        decimals,
        flags,
        stringPos,
        extTypeName,
        extTypeFormat) => new BigDecimalColumn(buf, charset,length, dataType, decimals, flags, stringPos, extTypeName, extTypeFormat);
    public static Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder> SignedTinyIntLamdba = (buf,
        charset,
        length,
        dataType,
        decimals,
        flags,
        stringPos,
        extTypeName,
        extTypeFormat) => new SignedTinyIntColumn(buf, charset,length, dataType, decimals, flags, stringPos, extTypeName, extTypeFormat);
    
    public static Func<IReadableByteBuf,int,long,DataType ,byte,int,int[] ,string ,string , IColumnDecoder> GuidLamdba = (buf,
        charset,
        length,
        dataType,
        decimals,
        flags,
        stringPos,
        extTypeName,
        extTypeFormat) => new GuidColumn(buf, charset,length, dataType, decimals, flags, stringPos, extTypeName, extTypeFormat);

    
    
}