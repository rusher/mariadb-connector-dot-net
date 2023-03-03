namespace Mariadb.utils.exception;

public class DbNonTransientConnectionException : SqlException
{
    public DbNonTransientConnectionException() : base() { }

    public DbNonTransientConnectionException(string? message) : base(message) { }

    public DbNonTransientConnectionException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbNonTransientConnectionException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
    public override bool IsTransient => false;
}