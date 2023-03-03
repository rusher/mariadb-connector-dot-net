using System.Data.Common;
using System.Text.RegularExpressions;
using Mariadb.client;
using Mariadb.client.impl;
using Mariadb.client.result;
using Mariadb.client.socket;
using Mariadb.client.util;
using Mariadb.message.server;
using Mariadb.utils;

namespace Mariadb.message;

internal abstract class AbstractClientMessage : IClientMessage
{
    public abstract int Encode(IWriter writer, IContext context);

    public uint BatchUpdateLength()
    {
        return 0;
    }

    public string Description()
    {
        return null;
    }

    public bool BinaryProtocol()
    {
        return false;
    }

    public bool CanSkipMeta()
    {
        return false;
    }

    public ICompletion ReadPacket(
        DbCommand stmt,
        int fetchSize,
        int resultSetType,
        bool closeOnCompletion,
        IReader reader,
        IWriter writer,
        IContext context,
        ExceptionFactory exceptionFactory,
        bool traceEnable,
        IClientMessage message)
    {
        var buf = reader.ReadReusablePacket(traceEnable);

        switch (buf.GetByte())
        {
            // *********************************************************************************************************
            // * OK response
            // *********************************************************************************************************
            case 0x00:
                return new OkPacket(buf, context);

            // *********************************************************************************************************
            // * ERROR response
            // *********************************************************************************************************
            case 0xff:
                // force current status to in transaction to ensure rollback/commit, since command may
                // have issue a transaction
                var errorPacket = new ErrorPacket(buf, context);
                throw exceptionFactory
                    .withSql(Description())
                    .create(
                        errorPacket.Message, errorPacket.SqlState, errorPacket.ErrorCode);
            case 0xfb:
                buf.Skip(1); // skip header
                Exception exception = null;
                reader.GetSequence().Value = 0x01;
                var inputStream = GetLocalInfileInputStream();
                if (inputStream == null)
                {
                    var fileName = buf.ReadStringNullEnd();
                    if (!ValidateLocalFileName(fileName, context))
                        exception =
                            exceptionFactory
                                .withSql(Description())
                                .create(
                                    $"LOAD DATA LOCAL INFILE asked for file '{fileName}' that doesn't correspond to initial query {Description()}. Possible malicious proxy changing server answer ! Command interrupted",
                                    "HY000");
                    else
                        try
                        {
                            inputStream = File.OpenRead(fileName);
                        }
                        catch (FileNotFoundException f)
                        {
                            exception =
                                exceptionFactory
                                    .withSql(Description())
                                    .create("Could not send file : " + f.Message, "HY000", f);
                        }
                }

                // sending stream
                if (inputStream != null)
                    try
                    {
                        var fileBuf = new byte[8192];
                        int len;
                        while ((len = inputStream.Read(fileBuf)) > 0)
                        {
                            writer.WriteBytes(fileBuf, 0, len);
                            writer.Flush();
                        }
                    }
                    finally
                    {
                        inputStream.Close();
                    }

                // after file send / having an error, sending an empty packet to keep connection state ok
                writer.WriteEmptyPacket();
                var completion =
                    ReadPacket(
                        stmt,
                        fetchSize,
                        resultSetType,
                        closeOnCompletion,
                        reader,
                        writer,
                        context,
                        exceptionFactory,
                        traceEnable,
                        message);
                if (exception != null) throw exception;
                return completion;

            // *********************************************************************************************************
            // * ResultSet
            // *********************************************************************************************************
            default:
                var fieldCount = buf.ReadIntLengthEncodedNotNull();

                IColumnDecoder[] ci;
                var canSkipMeta = context.canSkipMeta() && CanSkipMeta();
                var skipMeta = canSkipMeta ? buf.ReadByte() == 0 : false;
                if (canSkipMeta && skipMeta)
                {
                    ci = ((BasePreparedStatement)stmt).getMeta();
                }
                else
                {
                    // read columns information's
                    ci = new IColumnDecoder[fieldCount];
                    for (var i = 0; i < fieldCount; i++)
                        ci[i] =
                            IColumnDecoder.decode(
                                new StandardReadableByteBuf(reader.ReadPacket(traceEnable)),
                                context.isExtendedInfo());
                }

                if (canSkipMeta && !skipMeta) ((BasePreparedStatement)stmt).updateMeta(ci);

                // intermediate EOF
                if (!context.isEofDeprecated()) reader.SkipPacket();

                return new MariadbDataReader(
                    stmt,
                    BinaryProtocol(),
                    ci,
                    reader,
                    context,
                    resultSetType,
                    closeOnCompletion,
                    traceEnable);
        }
    }

    public Stream GetLocalInfileInputStream()
    {
        return null;
    }

    public bool ValidateLocalFileName(string fileName, IContext context)
    {
        return false;
    }

    private static bool ValidateLocalFileName(
        string sql, IParameters parameters, string fileName, IContext context)
    {
        var pattern =
            new Regex(
                "^(\\s*\\/\\*([^\\*]|\\*[^\\/])*\\*\\/)*\\s*LOAD\\s+(DATA|XML)\\s+((LOW_PRIORITY|CONCURRENT)\\s+)?LOCAL\\s+INFILE\\s+'"
                + fileName
                + "'",
                RegexOptions.IgnoreCase);
        if (pattern.Match(sql).Success) return true;

        if (parameters != null)
        {
            pattern =
                new Regex(
                    "^(\\s*\\/\\*([^\\*]|\\*[^\\/])*\\*\\/)*\\s*LOAD\\s+(DATA|XML)\\s+((LOW_PRIORITY|CONCURRENT)\\s+)?LOCAL\\s+INFILE\\s+\\?",
                    RegexOptions.IgnoreCase);
            if (pattern.Match(sql).Success && parameters.size() > 0)
            {
                var paramString = parameters.get(0).bestEffortStringValue(context);
                if (paramString != null)
                    return paramString.ToLowerInvariant().Equals("'" + fileName.ToLowerInvariant() + "'");
                return true;
            }
        }

        return false;
    }
}