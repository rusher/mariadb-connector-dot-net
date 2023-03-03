namespace Mariadb.utils.constant;

public class ColumnFlags
{
    
    /** must a column have non-null value only */
    public const ushort NOT_NULL = 1;

    /** Is column a primary key */
    public const ushort PRIMARY_KEY = 2;

    /** Is this column a unique key */
    public const ushort UNIQUE_KEY = 4;

    /** Is this column part of a multiple column key */
    public const ushort MULTIPLE_KEY = 8;

    /** Does this column contain blob */
    public const ushort BLOB = 16;

    /** Is column number value unsigned */
    public const ushort UNSIGNED = 32;

    /** Must number value be filled with Zero */
    public const ushort ZEROFILL = 64;

    /** Is binary value */
    public const ushort BINARY_COLLATION = 128;

    /** Is column of type enum */
    public const ushort ENUM = 256;

    /** Does column auto-increment */
    public const ushort AUTO_INCREMENT = 512;

    /** Is column of type Timestamp */
    public const ushort TIMESTAMP = 1024;

    /** Is column type set */
    public const ushort SET = 2048;

    /** Does column have no default value */
    public const ushort NO_DEFAULT_VALUE_FLAG = 4096;

}