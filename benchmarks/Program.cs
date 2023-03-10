using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Mariadb;
using MySqlConnector;

namespace Benchmark;

internal class Program
{
    private static void Main()
    {
        var customConfig = ManualConfig
            .Create(DefaultConfig.Instance)
            .AddValidator(JitOptimizationsValidator.FailOnError)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(StatisticColumn.AllStatistics)
            .AddJob(Job.Default.WithRuntime(CoreRuntime.Core70))
            .AddExporter(JsonExporter.Brief);

        var summary = BenchmarkRunner.Run<MySqlClient>(customConfig);
        Console.WriteLine(summary);
    }
}

[JsonExporter("-custom", true, true)]
public class MySqlClient
{
    protected static readonly string do1000Cmd;
    protected static ArrayList chars = new();

    // TODO: move to config file
    private static readonly string s_connectionString =
        "Protocol=tcp;server=127.0.0.1;Uid=root;Database=bench;database=bench;SslMode=none;Use Affected Rows=true;Connection Reset=false;Default Command Timeout=0;AutoEnlist=false";

    private readonly Dictionary<string, DbConnection> m_connections = new();


    static MySqlClient()
    {
        var sb = new StringBuilder("do ?");
        for (var i = 1; i < 1000; i++) sb.Append(",?");
        do1000Cmd = sb.ToString();

        chars.AddRange("123456789abcdefghijklmnop\\Z".ToCharArray());
        chars.Add("😎");
        chars.Add("🌶");
        chars.Add("🎤");
        chars.Add("🥂");
    }

    [Params("MySql.Data", "MySqlConnector", "MariaDbConnector")]
    public string Library { get; set; }
    //
    // [Benchmark]
    // public void executePrepareSelect1000RowsSync()
    // {
    //     int total = 0;
    //     using (var cmd = Connection.CreateCommand()) {
    //            cmd.CommandText = "select * from 1000rows";
    //            cmd.Prepare();
    // 		using var reader = cmd.ExecuteReader();
    //            while (reader.Read())
    //            {
    //                total += reader.GetInt32(0);
    //                reader.GetString(1);
    //            }
    //            reader.Close();
    // 	}
    // }
    //
    // [Benchmark]
    // public void executeSelect1Sync()
    // {
    //     using (var cmd = Connection.CreateCommand()) {
    //            cmd.CommandText = "select 1";
    //
    // 		using var reader = cmd.ExecuteReader();
    //            while (reader.Read())
    //            {
    //                reader.GetInt32(0);
    //            }
    //            reader.Close();
    // 	}
    // }
    //
    // [Benchmark]
    // public void executeInsertBatchSync()
    // {
    //        String s = randomString(100);
    //
    //        using (var cmd = Connection.CreateCommand()) {
    //            cmd.CommandText = "INSERT INTO perfTestTextBatch(t0) VALUES (?)";
    //            var param = cmd.CreateParameter();
    //            param.Value = s;
    //            param.DbType = DbType.String;
    //            cmd.Parameters.Add(param);
    //            for (int i = 0; i < 100; i++) {
    //                cmd.ExecuteNonQuery();
    //            }
    //        }
    // }
    //
    // [Benchmark]
    // public void executePrepareInsertBatchSync()
    // {
    //        String s = randomString(100);
    //
    //        using (var cmd = Connection.CreateCommand()) {
    //            cmd.CommandText = "INSERT INTO perfTestTextBatch(t0) VALUES (?)";
    //            var param = cmd.CreateParameter();
    //            param.Value = s;
    //            param.DbType = DbType.String;
    //            cmd.Parameters.Add(param);
    //            cmd.Prepare();
    //
    //            for (int i = 0; i < 100; i++) {
    //                cmd.ExecuteNonQuery();
    //            }
    //        }
    // }

    private DbConnection Connection { get; set; }

    public static string randomString(int length)
    {
        var result = new StringBuilder();
        var random = new Random();
        for (var i = length; i > 0; --i)
            result.Append(chars[random.Next(0, chars.Count - 1)]);
        return result.ToString();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var mySqlConnector = new MySqlConnection(s_connectionString);
        try
        {
            mySqlConnector.Open();
            m_connections.Add("MySqlConnector", mySqlConnector);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        var mySqlData = new MySql.Data.MySqlClient.MySqlConnection(s_connectionString);
        try
        {
            mySqlData.Open();
            m_connections.Add("MySql.Data", mySqlData);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        var mariaDbConnection = new MariaDbConnection(s_connectionString);
        try
        {
            mariaDbConnection.Open();
            m_connections.Add("MariaDbConnector", mariaDbConnection);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        Connection = m_connections[Library];
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        foreach (var connection in m_connections.Values)
            connection.Dispose();
        m_connections.Clear();
        // MySqlConnector.MySqlConnection.ClearAllPools();
        // MySql.Data.MySqlClient.MySqlConnection.ClearAllPools();
    }

    // [Benchmark]
    // public async Task Do1Async()
    // {
    // 	using var cmd = Connection.CreateCommand();
    // 	cmd.CommandText = "do 1";
    // 	await cmd.ExecuteNonQueryAsync();
    // }
    //
    // [Benchmark]
    // public void Do1Sync()
    // {
    // 	using var cmd = Connection.CreateCommand();
    // 	cmd.CommandText = "do 1";
    // 	cmd.ExecuteNonQuery();
    // }
    //
    //    [Benchmark]
    //    public void readAllRowsSync()
    //    {
    //        executeReadAllRowsSync();
    //    }
    //
    //        [Benchmark]
    //        public void readPrepareAllRowsSync()
    //        {
    //            executePrepareReadAllRowsSync();
    //        }
    //
    // [Benchmark]
    // public async Task readAllRowsAsync()
    // {
    // 	await executeReadAllRowsAsync();
    // }
    //
    // private async Task<int> executeReadAllRowsAsync()
    // {
    // 	int total = 0;
    // 	using (var cmd = Connection.CreateCommand())
    // 	{
    // 		cmd.CommandText = "select * FROM test100";
    // 		using var reader = await cmd.ExecuteReaderAsync();
    //            while (await reader.ReadAsync())
    //            {
    //                for (int i = 0; i < 100; i++) {
    //                    total += reader.GetInt32(i);
    //                }
    //            }
    //            reader.Close();
    // 	}
    // 	return total;
    // }
    //
    //    private int executeReadAllRowsSync()
    // {
    // 	int total = 0;
    // 	using (var cmd = Connection.CreateCommand())
    // 	{
    // 		cmd.CommandText = "select * FROM test100";
    // 		using var reader = cmd.ExecuteReader();
    //            while (reader.Read())
    //            {
    //                for (int i = 0; i < 100; i++) {
    //                    total += reader.GetInt32(i);
    //                }
    //            }
    //            reader.Close();
    // 	}
    // 	return total;
    // }
    //
    //    private int executePrepareReadAllRowsSync()
    // {
    // 	int total = 0;
    // 	using (var cmd = Connection.CreateCommand())
    // 	{
    // 		cmd.CommandText = "select * FROM test100";
    // 		cmd.Prepare();
    // 		using var reader = cmd.ExecuteReader();
    //            while (reader.Read())
    //            {
    //                for (int i = 0; i < 100; i++) {
    //                    total += reader.GetInt32(i);
    //                }
    //            }
    //            reader.Close();
    // 	}
    // 	return total;
    // }
    // [Benchmark]
    // public void executeDo1000CmdSync()
    // {
    //     using (var cmd = Connection.CreateCommand()) {
    //            cmd.CommandText = do1000Cmd;
    //
    //            for (int i = 0; i < 1000; i++) {
    //                var param = cmd.CreateParameter();
    //                param.Value = i;
    //                param.DbType = DbType.Int32;
    //    		    cmd.Parameters.Add(param);
    //            }
    //            cmd.Prepare();
    // 		cmd.ExecuteNonQuery();
    // 	}
    // }

    [Benchmark]
    public int executeSelect1000RowsSync()
    {
        var total = 0;
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = "select * from 1000rows";

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    total += reader.GetInt32(0);
                    reader.GetString(1);
                }
            }
            //reader.Close();
        }

        return total;
    }
}