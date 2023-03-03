namespace Mariadb.utils.exception;

public class DbSyntaxErrorException : DbNonTransientConnectionException
{
    public DbSyntaxErrorException() : base() { }

    public DbSyntaxErrorException(string? message) : base(message) { }

    public DbSyntaxErrorException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbSyntaxErrorException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
}