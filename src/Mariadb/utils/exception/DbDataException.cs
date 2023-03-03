namespace Mariadb.utils.exception;

public class DbDataException : DbNonTransientConnectionException
{
    public DbDataException() : base() { }

    public DbDataException(string? message) : base(message) { }

    public DbDataException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbDataException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
}