namespace Mariadb.utils.exception;

public class DbInvalidAuthorizationSpecException : DbNonTransientConnectionException
{
    public DbInvalidAuthorizationSpecException() : base() { }

    public DbInvalidAuthorizationSpecException(string? message) : base(message) { }

    public DbInvalidAuthorizationSpecException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbInvalidAuthorizationSpecException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
}