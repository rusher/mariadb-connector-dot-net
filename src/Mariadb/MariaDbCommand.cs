using System.Data;
using System.Data.Common;
using Mariadb.client.result;
using Mariadb.client.result.rowdecoder;
using Mariadb.message;
using Mariadb.message.client;
using Mariadb.message.server;
using Mariadb.utils;
using Mariadb.utils.constant;
using Mariadb.utils.exception;

namespace Mariadb;

public class MariaDbCommand : DbCommand
{
    private bool _closed;
    private MariaDbConnection _conn;
    private ICompletion _currResult;
    private readonly object _lock;
    private readonly PrepareResultPacket _prepare = null;
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
        set => _conn = (MariaDbConnection?)value;
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

    internal void CloseConnection()
    {
        Close();
        _conn.Close();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        lock (_lock)
        {
            try
            {
                var cmd = EscapeTimeout(CommandText);
                _results =
                    _conn.Client
                        .Execute(
                            new QueryPacket(cmd, LocalInfileInputStream),
                            this,
                            behavior,
                            false);
                _currResult = _results[0];
                _results.RemoveAt(0);
                if (_currResult is DbDataReader) return (DbDataReader)_currResult;
                return new CompleteDataReader(new IColumnDecoder[0], new byte[0][], _conn!.Client!.Context, behavior);
            }
            finally
            {
                LocalInfileInputStream = null;
            }
        }
    }

    public void Close()
    {
        if (!_closed)
        {
            _closed = true;

            if (_currResult != null && _currResult is AbstractDataReader)
                ((AbstractDataReader)_currResult).CloseFromCommandClose(_lock);

            // close result-set
            if (_results != null && _results.Any())
                foreach (var completion in _results)
                    if (completion is AbstractDataReader)
                        ((AbstractDataReader)completion).CloseFromCommandClose(_lock);
        }
    }

    private void CheckNotClosed()
    {
        if (_closed) throw new SqlException("Cannot do an operation on a closed statement");
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
            result.FetchRemaining();
            if (result.Streaming()
                && (_conn!.Client!.Context.ServerStatus & ServerStatus.MORE_RESULTS_EXISTS) > 0)
                _conn!.Client!.ReadStreamingResults(
                    _results, CommandBehavior.Default);
        }
    }

    private string EscapeTimeout(string sql)
    {
        if (CommandTimeout != 0 && _conn!.CanUseServerTimeout)
            return "SET STATEMENT max_statement_time=" + CommandTimeout + " FOR " + sql;

        return sql;
    }

    public override int ExecuteNonQuery()
    {
        throw new NotImplementedException();
    }

    public override object? ExecuteScalar()
    {
        throw new NotImplementedException();
    }

    public override void Prepare()
    {
        throw new NotImplementedException();
    }
}