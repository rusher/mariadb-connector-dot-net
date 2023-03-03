using System.Data.Common;
using System.Text;
using Mariadb.utils.exception;

namespace Mariadb.utils;

public class ExceptionFactory
{
    
  private readonly Configuration _conf;
  private readonly HostAddress _hostAddress;
  private MariaDbConnection _connection;
  private long _threadId;
  private DbCommand _command;
  private string? _sql;

  public string? Sql
  {
    get => _sql;
  }
  
  /**
   * Connection Exception factory constructor
   *
   * @param conf configuration
   * @param hostAddress current host
   */
  public ExceptionFactory(Configuration conf, HostAddress hostAddress) {
    _conf = conf;
    _hostAddress = hostAddress;
  }

  private ExceptionFactory(
    MariaDbConnection connection,
      Configuration conf,
      HostAddress hostAddress,
      long threadId,
    DbCommand command, string? sql) {
    _connection = connection;
    _conf = conf;
    _hostAddress = hostAddress;
    _threadId = threadId;
    _command = command;
    _sql = sql;
  }

  private static string buildMsgText(
      string initialMessage,
      long threadId,
      Configuration conf,
      string sql) {

    StringBuilder msg = new StringBuilder();

    if (threadId != 0L) {
      msg.Append("(conn=").Append(threadId).Append(") ");
    }
    msg.Append(initialMessage);

    if (conf.DumpQueriesOnException && sql != null) {
      if (conf.MaxQuerySizeToLog != 0 && sql.Length > conf.MaxQuerySizeToLog - 3) {
        msg.Append("\nQuery is: ").Append(sql, 0, (int) conf.MaxQuerySizeToLog - 3).Append("...");
      } else {
        msg.Append("\nQuery is: ").Append(sql);
      }
    }
    return msg.ToString();
  }

  public void setConnection(ExceptionFactory oldExceptionFactory) {
    _connection = oldExceptionFactory._connection;
  }

  public ExceptionFactory setConnection(MariaDbConnection connection) {
    _connection = connection;
    return this;
  }

  public void setThreadId(long threadId) {
    _threadId = threadId;
  }

  public ExceptionFactory of(DbCommand command) {
    return new ExceptionFactory(
        _connection,
        _conf,
        _hostAddress,
        _threadId,
        command,
        _sql);
  }

  public ExceptionFactory withSql(string sql)
  {
    return new ExceptionFactory(
        _connection,
        _conf,
        _hostAddress,
        _threadId,
        _command,
        sql);
  }

  private SqlException createException(
      string initialMessage, string sqlState, int errorCode, Exception cause) {

    string msg = buildMsgText(initialMessage, _threadId, _conf, _sql);

    if ("70100".Equals(sqlState)) { // ER_QUERY_INTERRUPTED
      return new DbTimeoutException(msg, errorCode, sqlState);
    }
    // 4166 : mariadb load data infile disable
    // 1148 : 10.2 mariadb load data infile disable
    // 3948 : mysql load data infile disable
    if ((errorCode == 4166 || errorCode == 3948 || errorCode == 1148) && !_conf.AllowLoadLocalInfile) {
      return new SqlException(
          "Local infile is disabled by connector. Enable `allowLocalInfile` to allow local infile commands",
          errorCode,
          sqlState);
    }

    SqlException returnEx;
    string sqlClass = sqlState == null ? "42" : sqlState.Substring(0, 2);
    switch (sqlClass) {
      case "0A":
        returnEx = new DbFeatureNotSupportedException(msg, errorCode, sqlState);
        break;
      case "22":
      case "26":
      case "2F":
      case "20":
      case "42":
      case "XA":
        returnEx = new DbSyntaxErrorException(msg, errorCode, sqlState);
        break;
      case "25":
      case "28":
        returnEx = new DbInvalidAuthorizationSpecException(msg, errorCode, sqlState);
        break;
      case "21":
      case "23":
        returnEx = new DbIntegrityConstraintViolationException(msg, errorCode, sqlState);
        break;
      case "08":
        returnEx = new DbNonTransientConnectionException(msg, errorCode, sqlState);
        break;
      case "40":
        returnEx = new DbTransactionRollbackException(msg, errorCode, sqlState);
        break;
      case "HY":
        returnEx = cause == null ? new SqlException(msg, errorCode, sqlState) : new SqlException(msg, cause);
        break;
      default:
        returnEx = cause == null ? new DbTransientConnectionException(msg, errorCode, sqlState) : new DbTransientConnectionException(msg, cause);
        break;
    }

    return returnEx;
  }

  private SqlException createException( string initialMessage, Exception cause) {
    string msg = buildMsgText(initialMessage, _threadId, _conf, _sql);
    return new DbTransientConnectionException(msg, cause);
  }
  
  public SqlException notSupported(string message) {
    return createException(message, "0A000", -1, null);
  }

  public SqlException create(string message) {
    return createException(message, "42000", -1, null);
  }

  public SqlException create(string message, string sqlState) {
    return createException(message, sqlState, -1, null);
  }

  public SqlException create(string message, string sqlState, Exception cause) {
    return createException(message, sqlState, -1, cause);
  }

  public SqlException create(string message, string sqlState, int errorCode) {
    return createException(message, sqlState, errorCode, null);
  }

  

}