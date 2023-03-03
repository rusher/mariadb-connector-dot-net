namespace Mariadb.utils.constant;

public class ServerStatus
{
    /**
     * is in transaction
     */
    public const ushort IN_TRANSACTION = 1;

    /**
     * autocommit
     */
    public const ushort AUTOCOMMIT = 2;

    /**
     * more result exists (packet follows)
     */
    public const ushort MORE_RESULTS_EXISTS = 8;

    /**
     * no good index was used
     */
    public const ushort QUERY_NO_GOOD_INDEX_USED = 16;

    /**
     * no index was used
     */
    public const ushort QUERY_NO_INDEX_USED = 32;

    /**
     * cursor exists
     */
    public const ushort CURSOR_EXISTS = 64;

    /**
     * last row sent
     */
    public const ushort LAST_ROW_SENT = 128;

    /**
     * database dropped
     */
    public const ushort DB_DROPPED = 256;

    /**
     * escape type
     */
    public const ushort NO_BACKSLASH_ESCAPES = 512;

    /**
     * metadata changed
     */
    public const ushort METADATA_CHANGED = 1024;

    /**
     * query was slow
     */
    public const ushort QUERY_WAS_SLOW = 2048;

    /**
     * resultset contains output parameters
     */
    public const ushort PS_OUT_PARAMETERS = 4096;

    /**
     * session state change (OK_Packet contains additional data)
     */
    public const ushort SERVER_SESSION_STATE_CHANGED = 1 << 14;
}