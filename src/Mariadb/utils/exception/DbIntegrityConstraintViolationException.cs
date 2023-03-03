namespace Mariadb.utils.exception;

public class DbIntegrityConstraintViolationException : DbNonTransientConnectionException
{
    public DbIntegrityConstraintViolationException() : base() { }

    public DbIntegrityConstraintViolationException(string? message) : base(message) { }

    public DbIntegrityConstraintViolationException(string? message, System.Exception? innerException) : base(message, innerException) { }
    
    public DbIntegrityConstraintViolationException(string? message, int errorCode, string sqlState) : base(message, errorCode, sqlState) { }
}