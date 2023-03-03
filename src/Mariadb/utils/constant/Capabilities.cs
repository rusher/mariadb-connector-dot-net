namespace Mariadb.utils.constant;

public class Capabilities
{
    
  /// Is client mysql
  public const uint CLIENT_MYSQL = 1;

  /// use Found rows instead of affected rows
  public const uint FOUND_ROWS = 2;

  /** Get all column flags */
  public const uint LONG_FLAG = 4;

  /** One can specify db on connect */
  public const uint CONNECT_WITH_DB = 8;

  /** Don't allow database.table.column */
  public const uint NO_SCHEMA = 16;

  /** use compression protocol */
  public const uint COMPRESS = 32;

  /** Odbc client */
  public const uint ODBC = 64;

  /** Can use LOAD DATA LOCAL */
  public const uint LOCAL_FILES = 128;

  /** Ignore spaces before '(' */
  public const uint IGNORE_SPACE = 256;

  /** Use 4.1 protocol */
  public const uint CLIENT_PROTOCOL_41 = 512;

  /** Is interactive client */
  public const uint CLIENT_INTERACTIVE = 1024;

  /** Switch to SSL after handshake */
  public const uint SSL = 2048;

  /** IGNORE sigpipes */
  public const uint IGNORE_SIGPIPE = 4096;

  /** transactions */
  public const uint TRANSACTIONS = 8192;

  /** reserved - not used */
  public const uint RESERVED = 16384;

  /** New 4.1 authentication */
  public const uint SECURE_CONNECTION = 32768;

  /** Enable/disable multi-stmt support */
  public const uint MULTI_STATEMENTS = 1 << 16;

  /** Enable/disable multi-results */
  public const uint MULTI_RESULTS = 1 << 17;

  /** Enable/disable multi-results for PrepareStatement */
  public const uint PS_MULTI_RESULTS = 1 << 18;

  /** Client supports plugin authentication */
  public const uint PLUGIN_AUTH = 1 << 19;

  /** Client send connection attributes */
  public const uint CONNECT_ATTRS = 1 << 20;

  /** authentication data length is a length auth integer */
  public const uint PLUGIN_AUTH_LENENC_CLIENT_DATA = 1 << 21;

  /** server send session tracking info */
  public const uint CLIENT_SESSION_TRACK = 1 << 23;

  /** EOF packet deprecated */
  public const uint CLIENT_DEPRECATE_EOF = 1 << 24;

  /** Client support progress indicator (before 10.2) */
  public const uint PROGRESS_OLD = 1 << 29;

  /* MariaDB specific capabilities */

  /** Client progression */
  public const ulong PROGRESS = 1L << 32;

  /** not used anymore - reserved */
  public const ulong MARIADB_RESERVED = 1L << 33;

  /** permit COM_STMT_BULK commands */
  public const ulong STMT_BULK_OPERATIONS = 1L << 34;

  /** metadata extended information */
  public const ulong EXTENDED_TYPE_INFO = 1L << 35;

  /** permit metadata caching */
  public const ulong CACHE_METADATA = 1L << 36;

}