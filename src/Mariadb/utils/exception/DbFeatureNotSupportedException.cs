namespace Mariadb.utils.exception;

public class DbFeatureNotSupportedException : DbNonTransientConnectionException
{
    public DbFeatureNotSupportedException() : base() { }

    public DbFeatureNotSupportedException(string? message) : base(message) { }

    public DbFeatureNotSupportedException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbFeatureNotSupportedException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
}