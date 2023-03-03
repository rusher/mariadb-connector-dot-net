using Mariadb.utils;

namespace Mariadb.client;

public interface IContext
{
    long getThreadId();

    byte[] getSeed();

    bool hasServerCapability(long flag);

    bool hasClientCapability(long flag);

    bool permitPipeline();

    int getServerStatus();

    void setServerStatus(int serverStatus);

    string getDatabase();

    void setDatabase(string database);

    IServerVersion getVersion();

    bool isEofDeprecated();

    bool canSkipMeta();

    bool isExtendedInfo();

    int getWarning();

    void setWarning(int warning);

    ExceptionFactory getExceptionFactory();

    Configuration getConf();

    int getTransactionIsolationLevel();

    void setTransactionIsolationLevel(int transactionIsolationLevel);

    //PrepareCache getPrepareCache();

    /**
     * Reset prepare cache (after a failover)
     */
    void resetPrepareCache();

    /**
   * return connection current state change flag
   *
   * @return connection current state change flag
   */
    int getStateFlag();

    /**
     * reset connection state change flag
     */
    void resetStateFlag();

    /**
   * Indicate connection state (for pooling)
   *
   * @param state indicate that some connection state has changed
   */
    void addStateFlag(int state);
}