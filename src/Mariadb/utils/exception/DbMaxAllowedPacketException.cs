namespace Mariadb.utils.exception;

public class DbMaxAllowedPacketException : DbNonTransientConnectionException
{
    public bool MustReconnect
    {
        get;
    }

    public DbMaxAllowedPacketException(string? message, bool mustReconnect) : base(message)
    {
        MustReconnect = mustReconnect;
    }

}