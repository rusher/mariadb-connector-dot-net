using System.Data;
using System.Data.Common;
using Mariadb.client;
using Mariadb.client.impl;
using Mariadb.utils;

namespace Mariadb;

public sealed class MariaDbConnection : DbConnection
{
    private Configuration _conf;
    private string _connectionString;

    public bool CanUseServerTimeout;
    internal IClient Client;

    public MariaDbConnection(string? connectionString)
    {
        State = ConnectionState.Closed;
        ConnectionString = connectionString ?? "";
        Lock = new object();
        Client = null;
    }

    internal object Lock { get; }
    internal ExceptionFactory ExceptionFactory { get; set; }

    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (State == ConnectionState.Closed)
            {
                _conf = new Configuration(value);
                _connectionString = value;
            }
            else
            {
                throw new InvalidOperationException("Cannot set ConnectionString while connection is used");
            }
        }
    }

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
        Client = StandardClient.BuildClient(_conf, Lock, HostAddress.From(_conf.Server, _conf.Port, true), false);
        ExceptionFactory = Client.ExceptionFactory.SetConnection(this);
    }

    protected override MariaDbCommand CreateDbCommand()
    {
        return new MariaDbCommand(this);
    }
}