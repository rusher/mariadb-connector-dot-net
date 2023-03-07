using System.Data;
using Mariadb.client.socket;
using Mariadb.message.server;

namespace Mariadb.client.result.rowdecoder;

public class CompleteDataReader : AbstractDataReader
{
    public CompleteDataReader(MariaDbCommand stmt, bool binaryProtocol, IColumnDecoder[] metaDataList, IReader reader,
        IContext context, bool traceEnable, CommandBehavior behavior) : base(stmt, binaryProtocol, metaDataList, reader,
        context, traceEnable, behavior)
    {
        _data = new byte[10][];
        while (ReadNext())
        {
        }

        Loaded = true;
    }

    public CompleteDataReader(IColumnDecoder[] metadataList, byte[][] data, IContext context, CommandBehavior behavior)
        : base(metadataList, data, context, behavior)
    {
    }

    internal override void FetchRemaining()
    {
    }

    internal override bool Streaming()
    {
        return false;
    }
}