using System.Data.Common;

namespace Mariadb;

public class Configuration
{
    public readonly bool AllowLoadLocalInfile;
    public readonly string Database;
    public readonly bool DumpQueriesOnException;
    public readonly uint MaxQuerySizeToLog;
    public readonly string Password;
    public readonly uint Port;

    public readonly string Server;
    public readonly string User;

    public Configuration(DbConnectionStringBuilder builder)
    {
        var parser = new Parser(builder);
        Server = parser.getStringOption(
            new[] { "Server", "Host", "Data Source", "DataSource", "Address", "Addr", "Network Address" },
            "localhost");
        Port = parser.getIntOption(new[] { "Port" }, 3306);
        User = parser.getStringOption(
            new[] { "User", "User ID", "UserID", "Username", "Uid", "User name" }, null);
        Password = parser.getStringOption(
            new[] { "Password", "pwd" }, null);
        Database = parser.getStringOption(
            new[] { "Database", "Initial Catalog" }, null);
        DumpQueriesOnException = parser.getBoolOption(
            new[] { "DumpQueriesOnException" }, false);
        MaxQuerySizeToLog = parser.getIntOption(new[] { "MaxQuerySizeToLog" }, 1024);
        AllowLoadLocalInfile = parser.getBoolOption(
            new[] { "AllowLoadLocalInfile", "Allow Load Local Infile" }, false);
    }
}

internal class Parser
{
    private readonly DbConnectionStringBuilder _builder;

    public Parser(DbConnectionStringBuilder builder)
    {
        _builder = builder;
    }

    public string getStringOption(string[] keyAliases, string defaultValue)
    {
        foreach (var k in keyAliases)
        {
            var obj = _builder[k];
            if (obj == null) continue;
            return obj.ToString();
        }

        return defaultValue;
    }

    public uint getIntOption(string[] keyAliases, uint defaultValue)
    {
        foreach (var k in keyAliases)
        {
            var obj = _builder[k];
            if (obj == null) continue;
            if (obj is uint ui) return ui;
            if (obj is int i) return (uint)i;
            if (obj is string str)
                if (uint.TryParse(str, out var parseRes))
                    return parseRes;

            throw new ArgumentException($"Parameter {k} has wrong int32 value '{obj}'.");
        }

        return defaultValue;
    }

    public bool getBoolOption(string[] keyAliases, bool defaultValue)
    {
        foreach (var k in keyAliases)
        {
            var obj = _builder[k];
            if (obj == null) continue;
            if (obj is bool b) return b;
            if (obj is string str)
                if (bool.TryParse(str, out var parseRes))
                    return parseRes;

            throw new ArgumentException($"Parameter {k} has wrong boolean value '{obj}'.");
        }

        return defaultValue;
    }
}