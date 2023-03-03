using Mariadb.client.socket;

namespace Mariadb.client.util;

public interface IParameter
{
    void encodeText(IWriter encoder, IContext context);
    void encodeBinary(IWriter encoder);
    void encodeLongData(IWriter encoder);
    byte[] encodeData();
    bool canEncodeLongData();
    int getBinaryEncodeType();
    bool isNull();
    string bestEffortStringValue(IContext context);
}