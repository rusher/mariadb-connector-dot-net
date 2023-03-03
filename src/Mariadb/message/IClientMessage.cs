using System.Data.Common;
using Mariadb.client;
using Mariadb.client.socket;
using Mariadb.utils;

namespace Mariadb.message;

public interface IClientMessage
{
    int Encode(IWriter writer, IContext context);

    uint BatchUpdateLength();

    string Description();


    bool BinaryProtocol();

    bool CanSkipMeta();

    ICompletion ReadPacket(
        DbCommand stmt,
        int fetchSize,
        int resultSetType,
        bool closeOnCompletion,
        IReader reader,
        IWriter writer,
        IContext context,
        ExceptionFactory exceptionFactory,
        bool traceEnable,
        IClientMessage message);

    Stream GetLocalInfileInputStream();
}