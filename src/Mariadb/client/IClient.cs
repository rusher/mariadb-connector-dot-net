using System.Data.Common;
using Mariadb.message;
using Mariadb.utils;

namespace Mariadb.client;

public interface IClient
{
    List<ICompletion> execute(IClientMessage message, bool canRedo);

    List<ICompletion> execute(IClientMessage message, DbCommand stmt, bool canRedo);

    List<ICompletion> execute(
        IClientMessage message,
        DbCommand stmt,
        int fetchSize,
        long maxRows,
        int resultSetType,
        bool closeOnCompletion,
        bool canRedo);

    List<ICompletion> executePipeline(
        IClientMessage[] messages,
        DbCommand stmt,
        int fetchSize,
        long maxRows,
        int resultSetType,
        bool closeOnCompletion,
        bool canRedo);

    void readStreamingResults(
        List<ICompletion> completions,
        int fetchSize,
        long maxRows,
        int resultSetType,
        bool closeOnCompletion);

    //void closePrepare(Prepare prepare);

    void close();

    void setReadOnly(bool readOnly);

    int getSocketTimeout();

    void setSocketTimeout(int milliseconds);

    bool isClosed();

    /**
     * Reset connection
     */
    void reset();

    bool isPrimary();

    IContext getContext();

    ExceptionFactory getExceptionFactory();

    HostAddress getHostAddress();
}