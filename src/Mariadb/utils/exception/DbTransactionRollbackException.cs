namespace Mariadb.utils.exception;

public class DbTransactionRollbackException : DbTransientConnectionException
{
    public DbTransactionRollbackException() : base() { }

    public DbTransactionRollbackException(string? message) : base(message) { }

    public DbTransactionRollbackException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbTransactionRollbackException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
}