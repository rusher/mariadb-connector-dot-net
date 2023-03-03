namespace Mariadb.utils.exception;

public class DbTransientConnectionException : SqlException
{
    public DbTransientConnectionException() : base() { }

    public DbTransientConnectionException(string? message) : base(message) { }

    public DbTransientConnectionException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbTransientConnectionException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
    public override bool IsTransient => true;
}