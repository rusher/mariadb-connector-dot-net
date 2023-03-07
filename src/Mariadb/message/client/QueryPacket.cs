using Mariadb.client;
using Mariadb.client.socket;

namespace Mariadb.message.client;

public class QueryPacket : AbstractClientMessage
{
    private readonly Stream LocalInfileInputStream;

    private readonly string Sql;

    public QueryPacket(string sql, Stream localInfileInputStream = null)
    {
        Sql = sql;
        LocalInfileInputStream = localInfileInputStream;
    }

    public override string Description => Sql;

    public uint BatchUpdateLength()
    {
        return 1;
    }

    public override int Encode(IWriter writer, IContext context)
    {
        writer.InitPacket();
        writer.WriteByte(0x03);
        writer.WriteString(Sql);
        writer.Flush();
        return 1;
    }

    public bool IsCommit()
    {
        return string.Compare("COMMIT", Sql, StringComparison.OrdinalIgnoreCase) == 0;
    }

    public bool ValidateLocalFileName(string fileName, IContext context)
    {
        return ValidateLocalFileName(Sql, null, fileName, context);
    }
}