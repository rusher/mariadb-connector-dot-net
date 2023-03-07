using Mariadb.client;
using Mariadb.client.impl;
using Mariadb.client.socket;
using Mariadb.client.util;
using Mariadb.utils.constant;
using Mariadb.utils.log;

namespace Mariadb.message.server;

public class PrepareResultPacket : ICompletion, IPrepare
{
    private static readonly IColumnDecoder CONSTANT_PARAMETER;
    private static readonly Ilogger _logger = Loggers.getLogger("PrepareResultPacket");

    static PrepareResultPacket()
    {
        byte[] bytes =
        {
            0x03,
            0x64,
            0x65,
            0x66,
            0x00,
            0x00,
            0x00,
            0x01,
            0x3F,
            0x00,
            0x00,
            0x0C,
            0x3F,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x06,
            0x80,
            0x00,
            0x00,
            0x00,
            0x00
        };
        CONSTANT_PARAMETER =
            IColumnDecoder.Decode(new StandardReadableByteBuf(bytes, bytes.Length), true);
    }

    public PrepareResultPacket(IReadableByteBuf buf, IReader reader, IContext context)
    {
        var trace = _logger.isTraceEnabled();
        buf.ReadByte(); /* skip COM_STMT_PREPARE_OK */
        StatementId = buf.ReadUnsignedInt();
        int numColumns = buf.ReadUnsignedShort();
        int numParams = buf.ReadUnsignedShort();
        Parameters = new IColumnDecoder[numParams];
        Columns = new IColumnDecoder[numColumns];

        if (numParams > 0)
        {
            for (var i = 0; i < numParams; i++)
            {
                // skipping packet, since there is no metadata information.
                // might change when https://jira.mariadb.org/browse/MDEV-15031 is done
                Parameters[i] = CONSTANT_PARAMETER;
                reader.SkipPacket();
            }

            if (!context.EofDeprecated) reader.SkipPacket();
        }

        if (numColumns > 0)
        {
            for (var i = 0; i < numColumns; i++)
                Columns[i] =
                    IColumnDecoder.Decode(
                        new StandardReadableByteBuf(reader.ReadPacket(trace)),
                        context.HasClientCapability(Capabilities.EXTENDED_TYPE_INFO));
            if (!context.EofDeprecated) reader.SkipPacket();
        }
    }

    public uint StatementId { get; }
    public IColumnDecoder[] Parameters { get; }
    public IColumnDecoder[] Columns { get; set; }

    public void Close(IClient con)
    {
        con.ClosePrepare(this);
    }

    public void DecrementUse(IClient con, MariaDbCommand dbCommand)
    {
        Close(con);
    }

    public void IncrementUse(MariaDbCommand dbCommand)
    {
    }

    public void UnCache(IClient client)
    {
    }
}