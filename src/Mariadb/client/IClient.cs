using System.Data;
using Mariadb.client.util;
using Mariadb.message;
using Mariadb.utils;

namespace Mariadb.client;

public interface IClient
{
    IContext? Context { get; set; }

    ExceptionFactory ExceptionFactory { get; }
    HostAddress HostAddress { get; }

    List<ICompletion> Execute(IClientMessage message, bool canRedo);

    List<ICompletion> Execute(IClientMessage message, MariaDbCommand stmt, bool canRedo);

    List<ICompletion> Execute(
        IClientMessage message,
        MariaDbCommand stmt,
        CommandBehavior behavior,
        bool canRedo);

    List<ICompletion> ExecutePipeline(
        IClientMessage[] messages,
        MariaDbCommand stmt,
        CommandBehavior behavior,
        bool canRedo);

    void ReadStreamingResults(
        List<ICompletion> completions,
        CommandBehavior behavior);

    void ClosePrepare(IPrepare prepare);

    void Close();

    void SetReadOnly(bool readOnly);

    bool IsClosed();

    void Reset();

    bool IsPrimary();
}