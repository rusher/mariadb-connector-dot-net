using System.Data;
using System.Data.Common;

namespace Mariadb;

public sealed class MariaDbConnection : DbConnection
{
    public MariaDbConnection(string? connectionString)
    {
        ConnectionString = connectionString ?? "";
        State = ConnectionState.Closed;
    }

    public override string ConnectionString { get; set; }
    public override string Database { get; }
    public override ConnectionState State { get; }
    public override string DataSource { get; }
    public override string ServerVersion { get; }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new NotImplementedException();
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotImplementedException();
    }

    public override void Close()
    {
        throw new NotImplementedException();
    }

    public override void Open()
    {
        if (State != ConnectionState.Closed)
            throw new InvalidOperationException($"Cannot Open: State is {State}.");
        var builder = new DbConnectionStringBuilder();
        builder.ConnectionString = ConnectionString;

        var options = new Configuration(builder);
    }

    protected override DbCommand CreateDbCommand()
    {
        throw new NotImplementedException();
    }
}