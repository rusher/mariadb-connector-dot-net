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

    Task<List<ICompletion>> Execute(CancellationToken cancellationToken, IClientMessage message, bool canRedo);

    Task<List<ICompletion>> Execute(CancellationToken cancellationToken, IClientMessage message, MariaDbCommand stmt,
        bool canRedo);

    Task<List<ICompletion>> Execute(
        CancellationToken cancellationToken,
        IClientMessage message,
        MariaDbCommand stmt,
        CommandBehavior behavior,
        bool canRedo);

    Task<List<ICompletion>> ExecutePipeline(
        CancellationToken cancellationToken,
        IClientMessage[] messages,
        MariaDbCommand stmt,
        CommandBehavior behavior,
        bool canRedo);

    Task ReadStreamingResults(
        CancellationToken cancellationToken,
        List<ICompletion> completions,
        CommandBehavior behavior);

    Task ClosePrepare(IPrepare prepare);

    Task CloseAsync();

    void SetReadOnly(bool readOnly);

    bool IsClosed();

    void Reset();

    bool IsPrimary();
}