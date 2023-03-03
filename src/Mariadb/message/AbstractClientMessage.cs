using System.Data.Common;
using System.Text.RegularExpressions;
using Mariadb.client;
using Mariadb.client.impl;
using Mariadb.client.result;
using Mariadb.client.socket;
using Mariadb.client.util;
using Mariadb.message.server;
using Mariadb.utils;
using Mariadb.utils.constant;

namespace Mariadb.message;

abstract class AbstractClientMessage: IClientMessage
{
  public abstract int Encode(IWriter writer, IContext context);  
  public uint BatchUpdateLength() {
    return 0;
  }

  public string Description() {
    return null;
  }

  public bool BinaryProtocol() {
    return false;
  }

  public bool CanSkipMeta() {
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
      IClientMessage message) {

    IReadableByteBuf buf = reader.ReadReusablePacket(traceEnable);

    switch (buf.GetByte()) {

        // *********************************************************************************************************
        // * OK response
        // *********************************************************************************************************
      case (byte) 0x00:
        return new OkPacket(buf, context);

        // *********************************************************************************************************
        // * ERROR response
        // *********************************************************************************************************
      case (byte) 0xff:
        // force current status to in transaction to ensure rollback/commit, since command may
        // have issue a transaction
        ErrorPacket errorPacket = new ErrorPacket(buf, context);
        throw exceptionFactory
            .withSql(Description())
            .create(
                errorPacket.Message, errorPacket.SqlState, errorPacket.ErrorCode);
      case (byte) 0xfb:
        buf.Skip(1); // skip header
        Exception exception = null;
        reader.GetSequence().Value = 0x01;
        Stream inputStream = GetLocalInfileInputStream();
        if (inputStream == null) {
          String fileName = buf.ReadStringNullEnd();
          if (!ValidateLocalFileName(fileName, context)) {
            exception =
                exceptionFactory
                    .withSql(Description())
                    .create(
                            $"LOAD DATA LOCAL INFILE asked for file '{fileName}' that doesn't correspond to initial query {Description()}. Possible malicious proxy changing server answer ! Command interrupted",
                        "HY000");
          } else {

            try {
              inputStream = File.OpenRead(fileName);
            } catch (FileNotFoundException f) {
              exception =
                  exceptionFactory
                      .withSql(Description())
                      .create("Could not send file : " + f.Message, "HY000", f);
            }
          }
        }

        // sending stream
        if (inputStream != null) {
          try {
            byte[] fileBuf = new byte[8192];
            int len;
            while ((len = inputStream.Read(fileBuf)) > 0) {
              writer.WriteBytes(fileBuf, 0, len);
              writer.Flush();
            }
          } finally {
            inputStream.Close();
          }
        }

        // after file send / having an error, sending an empty packet to keep connection state ok
        writer.WriteEmptyPacket();
        ICompletion completion =
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
        if (exception != null) {
          throw exception;
        }
        return completion;

        // *********************************************************************************************************
        // * ResultSet
        // *********************************************************************************************************
      default:
        int fieldCount = buf.ReadIntLengthEncodedNotNull();

        IColumnDecoder[] ci;
        bool canSkipMeta = context.canSkipMeta() && CanSkipMeta();
        bool skipMeta = canSkipMeta ? buf.ReadByte() == 0 : false;
        if (canSkipMeta && skipMeta) {
          ci = ((BasePreparedStatement) stmt).getMeta();
        } else {
          // read columns information's
          ci = new IColumnDecoder[fieldCount];
          for (int i = 0; i < fieldCount; i++) {
            ci[i] =
                IColumnDecoder.decode(
                    new StandardReadableByteBuf(reader.ReadPacket(traceEnable)),
                    context.isExtendedInfo());
          }
        }
        if (canSkipMeta && !skipMeta) ((BasePreparedStatement) stmt).updateMeta(ci);

        // intermediate EOF
        if (!context.isEofDeprecated()) {
          reader.SkipPacket();
        }

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

  public Stream GetLocalInfileInputStream() {
    return null;
  }

  public bool ValidateLocalFileName(string fileName, IContext context) {
    return false;
  }

  private static bool ValidateLocalFileName(
      string sql, IParameters parameters, string fileName, IContext context) {
    Regex pattern =
      new Regex(
            "^(\\s*\\/\\*([^\\*]|\\*[^\\/])*\\*\\/)*\\s*LOAD\\s+(DATA|XML)\\s+((LOW_PRIORITY|CONCURRENT)\\s+)?LOCAL\\s+INFILE\\s+'"
                + fileName
                + "'",
            RegexOptions.IgnoreCase);
    if (pattern.Match(sql).Success) {
      return true;
    }

    if (parameters != null) {
      pattern =
        new Regex(
              "^(\\s*\\/\\*([^\\*]|\\*[^\\/])*\\*\\/)*\\s*LOAD\\s+(DATA|XML)\\s+((LOW_PRIORITY|CONCURRENT)\\s+)?LOCAL\\s+INFILE\\s+\\?",
              RegexOptions.IgnoreCase);
      if (pattern.Match(sql).Success && parameters.size() > 0) {
        string paramString = parameters.get(0).bestEffortStringValue(context);
        if (paramString != null) {
          return paramString.ToLowerInvariant().Equals("'" + fileName.ToLowerInvariant() + "'");
        }
        return true;
      }
    }
    return false;
  }
}