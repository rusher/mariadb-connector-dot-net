using System.Data;
using System.Data.Common;
using Mariadb.client.result;
using Mariadb.message;
using Mariadb.message.client;
using Mariadb.message.server;
using Mariadb.utils;
using Mariadb.utils.exception;

namespace Mariadb;

public class MariaDbCommand : DbCommand
{
    private readonly PrepareResultPacket _prepare = null;
    private bool _closed;
    private MariaDbConnection _conn;
    private ICompletion _currResult;
    private SemaphoreSlim _lock;
    private List<ICompletion> _results;
    public Stream LocalInfileInputStream;

    public MariaDbCommand(MariaDbConnection? dbConnection)
    {
        _conn = dbConnection;
        _lock = dbConnection!.Lock;
    }

    protected override DbConnection? DbConnection
    {
        get => _conn;
        set
        {
            _conn = (MariaDbConnection?)value;
            _lock = _conn!.Lock;
        }
    }

    public override string CommandText { get; set; }

    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    protected override DbParameterCollection DbParameterCollection { get; }
    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }


    public override void Cancel()
    {
        throw new NotImplementedException();
    }

    protected override DbParameter CreateDbParameter()
    {
        throw new NotImplementedException();
    }

    internal async Task CloseConnection()
    {
        await CloseAsync();
        await _conn.CloseAsync();
    }

    public async Task CloseAsync()
    {
        if (!_closed)
        {
            _closed = true;

            if (_currResult != null && _currResult is AbstractDataReader)
                await ((AbstractDataReader)_currResult).CloseFromCommandClose(_lock);

            // close result-set
            if (_results != null && _results.Any())
                foreach (var completion in _results)
                    if (completion is AbstractDataReader)
                        await ((AbstractDataReader)completion).CloseFromCommandClose(_lock);
        }
    }

    private void CheckNotClosed()
    {
        if (_closed) throw new SqlException("Cannot do an operation on a closed statement");
        if (_conn == null)
            throw new SqlException("Cannot do an operation without connection set");
        if (_conn.State != ConnectionState.Open)
            throw new SqlException(
                $"Cannot do an operation without open connection (State is {_conn.State.ToString()})");
        if (_conn.Client.IsClosed()) throw new SqlException("Cannot do an operation on closed connection");
        if (CommandText == null) throw new SqlException("CommandText need to be set to be execute");
    }


    private ExceptionFactory ExceptionFactory()
    {
        return _conn!.ExceptionFactory!.Of(this);
    }


    public IColumnDecoder[] _getMeta()
    {
        return _prepare.Columns;
    }

    internal void UpdateMeta(IColumnDecoder[] cols)
    {
        _prepare.Columns = cols;
    }

    internal void FetchRemaining()
    {
        if (_currResult != null && _currResult is AbstractDataReader)
        {
            var result = (AbstractDataReader)_currResult;
            //TODO diego fetch
            // result.FetchRemaining();
            // if (result.Streaming()
            //     && (_conn!.Client!.Context.ServerStatus & ServerStatus.MORE_RESULTS_EXISTS) > 0)
            //     _conn!.Client!.ReadStreamingResults(
            //         _results, CommandBehavior.Default);
        }
    }

    private string EscapeTimeout(string sql)
    {
        if (CommandTimeout != 0 && _conn!.CanUseServerTimeout)
            return "SET STATEMENT max_statement_time=" + CommandTimeout + " FOR " + sql;

        return sql;
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        await ExecuteInternal(cancellationToken, CommandBehavior.Default);
        if (_currResult is DbDataReader) return (DbDataReader)_currResult;
        return new StreamingDataReader(new IColumnDecoder[0], new byte[0], _conn!.Client!.Context, behavior);
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return ExecuteDbDataReaderAsync(behavior, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override int ExecuteNonQuery()
    {
        return ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        await ExecuteInternal(cancellationToken, CommandBehavior.Default);
        if (_currResult is DbDataReader)
            throw ExceptionFactory()
                .Create("the given SQL statement produces an unexpected ResultSet object", "HY000");
        return (int)((OkPacket)_currResult).AffectedRows;
    }

    private async Task ExecuteInternal(CancellationToken cancellationToken, CommandBehavior behavior)
    {
        CheckNotClosed();
        await _lock.WaitAsync();
        try
        {
            var cmd = EscapeTimeout(CommandText);
            _results =
                await _conn.Client
                    .Execute(
                        cancellationToken,
                        new QueryPacket(cmd, LocalInfileInputStream),
                        this,
                        behavior,
                        false);
            _currResult = _results[0];
            _results.RemoveAt(0);
        }
        finally
        {
            LocalInfileInputStream = null;
            _lock.Release();
        }
    }

    public override object? ExecuteScalar()
    {
        throw new NotImplementedException();
    }

    public override void Prepare()
    {
        throw new NotImplementedException();
    }

    private enum ParsingType
    {
        NoParameter,
        ClientSide,
        ServerSide
    }
}