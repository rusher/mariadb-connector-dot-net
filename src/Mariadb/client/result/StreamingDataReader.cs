using System.Data;
using Mariadb.client.socket;
using Mariadb.message.server;

namespace Mariadb.client.result;

public class StreamingDataReader : AbstractDataReader
{
    private readonly object _lock;
    private int _dataFetchTime;

    public StreamingDataReader(MariaDbCommand stmt, bool binaryProtocol, IColumnDecoder[] metaDataList, IReader reader,
        IContext context, bool traceEnable, object lockObj, CommandBehavior behavior) : base(stmt, binaryProtocol,
        metaDataList, reader, context,
        traceEnable, behavior)
    {
        _data = new byte[10][];
        _lock = lockObj;
        _dataFetchTime = 0;
        AddStreamingValue();
    }

    private void AddStreamingValue()
    {
        lock (_lock)
        {
            try
            {
                // read only fetchSize values
                ReadNext();
                _dataFetchTime++;
            }
            catch (IOException ioe)
            {
                throw _exceptionFactory.Create("Error while streaming resultSet data", "08000", ioe);
            }
        }
    }

    internal override void FetchRemaining()
    {
        if (!Loaded)
        {
            while (!Loaded) AddStreamingValue();
            _dataFetchTime++;
        }
    }

    internal override bool Streaming()
    {
        return true;
    }
}