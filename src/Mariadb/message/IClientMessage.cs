using System.Data;
using Mariadb.client;
using Mariadb.client.socket;
using Mariadb.utils;

namespace Mariadb.message;

public interface IClientMessage
{
    public string Description { get; }
    Task<int> Encode(CancellationToken cancellationToken, IWriter writer, IContext context);

    uint BatchUpdateLength();


    bool BinaryProtocol();

    bool CanSkipMeta();

    Task<ICompletion> ReadPacket(
        CancellationToken cancellationToken,
        MariaDbCommand stmt,
        CommandBehavior behavior,
        IReader reader,
        IWriter writer,
        IContext context,
        ExceptionFactory exceptionFactory,
        bool traceEnable,
        SemaphoreSlim lockObj,
        IClientMessage message);

    Stream GetLocalInfileInputStream();
}