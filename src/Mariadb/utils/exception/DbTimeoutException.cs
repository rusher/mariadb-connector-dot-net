namespace Mariadb.utils.exception;

public class DbTimeoutException : DbTransientConnectionException
{
    public DbTimeoutException() : base() { }

    public DbTimeoutException(string? message) : base(message) { }

    public DbTimeoutException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbTimeoutException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
}