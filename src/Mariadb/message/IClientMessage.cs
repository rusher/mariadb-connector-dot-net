using System.Data;
using Mariadb.client;
using Mariadb.client.socket;
using Mariadb.utils;

namespace Mariadb.message;

public interface IClientMessage
{
    public string Description { get; }
    int Encode(IWriter writer, IContext context);

    uint BatchUpdateLength();


    bool BinaryProtocol();

    bool CanSkipMeta();

    ICompletion ReadPacket(
        MariaDbCommand stmt,
        CommandBehavior behavior,
        IReader reader,
        IWriter writer,
        IContext context,
        ExceptionFactory exceptionFactory,
        bool traceEnable,
        object lockObj,
        IClientMessage message);

    Stream GetLocalInfileInputStream();
}