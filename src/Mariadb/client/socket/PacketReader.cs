using Mariadb.client.impl;
using Mariadb.client.util;
using Mariadb.utils.log;

namespace Mariadb.client.socket;

public class PacketReader : IReader
{
    private const int REUSABLE_BUFFER_LENGTH = 1024;
    private const int MAX_PACKET_SIZE = 0xffffff;
    private static readonly Ilogger logger = Loggers.getLogger("PacketReader");

    private readonly byte[] _header = new byte[4];
    private readonly uint _maxQuerySizeToLog;

    private readonly IReadableByteBuf _readBuf = new StandardReadableByteBuf(null, 0);
    private readonly byte[] _reusableArray = new byte[REUSABLE_BUFFER_LENGTH];

    private readonly MutableByte _sequence;
    private readonly Stream _stream;
    private string _serverThreadLog = "";

    public PacketReader(Stream stream, Configuration conf, MutableByte sequence)
    {
        _stream = stream;
        _maxQuerySizeToLog = conf.MaxQuerySizeToLog;
        _sequence = sequence;
    }

    public IReadableByteBuf ReadableBufFromArray(byte[] buf)
    {
        _readBuf.Buf(buf, buf.Length, 0);
        return _readBuf;
    }

    public async Task<IReadableByteBuf> ReadReusablePacket(CancellationToken cancellationToken)
    {
        return await ReadReusablePacket(cancellationToken, logger.isTraceEnabled());
    }

    public async Task<IReadableByteBuf> ReadReusablePacket(CancellationToken cancellationToken, bool traceEnable)
    {
        // ***************************************************
        // Read 4 byte header
        // ***************************************************
        var remaining = 4;
        var off = 0;
        do
        {
            var count = await _stream.ReadAsync(_header, off, remaining, cancellationToken);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + off
                    + " bytes from 4 (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        var lastPacketLength =
            (_header[0] & 0xff) + ((_header[1] & 0xff) << 8) + ((_header[2] & 0xff) << 16);
        _sequence.Value = _header[3];

        // prepare array
        byte[] rawBytes;
        if (lastPacketLength < REUSABLE_BUFFER_LENGTH)
            rawBytes = _reusableArray;
        else
            rawBytes = new byte[lastPacketLength];

        // ***************************************************
        // Read content
        // ***************************************************
        remaining = lastPacketLength;
        off = 0;
        do
        {
            var count = await _stream.ReadAsync(rawBytes, off, remaining, cancellationToken);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + (lastPacketLength - remaining)
                    + " bytes from "
                    + lastPacketLength
                    + " (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        if (traceEnable)
            logger.trace(
                $"read: {_serverThreadLog}\n{LoggerHelper.Hex(_header, rawBytes, 0, lastPacketLength, _maxQuerySizeToLog)}");

        _readBuf.Buf(rawBytes, lastPacketLength, 0);
        return _readBuf;
    }

    public async Task<byte[]> ReadPacket(CancellationToken cancellationToken, bool traceEnable)
    {
        // ***************************************************
        // Read 4 byte header
        // ***************************************************
        var remaining = 4;
        var off = 0;
        do
        {
            var count = await _stream.ReadAsync(_header, off, remaining, cancellationToken);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + off
                    + " bytes from 4 (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        var lastPacketLength =
            (_header[0] & 0xff) + ((_header[1] & 0xff) << 8) + ((_header[2] & 0xff) << 16);

        // prepare array
        var rawBytes = new byte[lastPacketLength];

        // ***************************************************
        // Read content
        // ***************************************************
        remaining = lastPacketLength;
        off = 0;
        do
        {
            var count = await _stream.ReadAsync(rawBytes, off, remaining, cancellationToken);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + (lastPacketLength - remaining)
                    + " bytes from "
                    + lastPacketLength
                    + " (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        if (traceEnable)
            logger.trace(
                $"read: {_serverThreadLog}\n{LoggerHelper.Hex(_header, rawBytes, 0, lastPacketLength, _maxQuerySizeToLog)}");

        // ***************************************************
        // In case content length is big, content will be separate in many 16Mb packets
        // ***************************************************
        if (lastPacketLength == MAX_PACKET_SIZE)
        {
            int packetLength;
            do
            {
                remaining = 4;
                off = 0;
                do
                {
                    var count = await _stream.ReadAsync(_header, off, remaining, cancellationToken);
                    if (count < 0) throw new IOException("unexpected end of stream, read " + off + " bytes from 4");
                    remaining -= count;
                    off += count;
                } while (remaining > 0);

                packetLength = (_header[0] & 0xff) + ((_header[1] & 0xff) << 8) + ((_header[2] & 0xff) << 16);

                var currentBufLength = rawBytes.Length;
                var newRawBytes = new byte[currentBufLength + packetLength];
                Array.Copy(rawBytes, 0, newRawBytes, 0, currentBufLength);
                rawBytes = newRawBytes;

                // ***************************************************
                // Read content
                // ***************************************************
                remaining = packetLength;
                off = currentBufLength;
                do
                {
                    var count = await _stream.ReadAsync(rawBytes, off, remaining, cancellationToken);
                    if (count < 0)
                        throw new IOException(
                            "unexpected end of stream, read "
                            + (packetLength - remaining)
                            + " bytes from "
                            + packetLength);
                    remaining -= count;
                    off += count;
                } while (remaining > 0);

                if (traceEnable)
                    logger.trace(
                        $"read: {_serverThreadLog}\n{LoggerHelper.Hex(_header, rawBytes, currentBufLength, packetLength, _maxQuerySizeToLog)}");

                lastPacketLength += packetLength;
            } while (packetLength == MAX_PACKET_SIZE);
        }

        return rawBytes;
    }

    public async Task SkipPacket(CancellationToken cancellationToken)
    {
        if (logger.isTraceEnabled())
        {
            await ReadReusablePacket(cancellationToken, logger.isTraceEnabled());
            return;
        }

        // ***************************************************
        // Read 4 byte header
        // ***************************************************
        var remaining = 4;
        var off = 0;
        do
        {
            var count = await _stream.ReadAsync(_header, off, remaining, cancellationToken);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + off
                    + " bytes from 4 (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        var lastPacketLength =
            (_header[0] & 0xff) + ((_header[1] & 0xff) << 8) + ((_header[2] & 0xff) << 16);

        remaining = lastPacketLength;
        // skipping 
        do
        {
            var count = await _stream.ReadAsync(_header, 0, Math.Min(4, remaining), cancellationToken);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, skipping bytes (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        // ***************************************************
        // In case content length is big, content will be separate in many 16Mb packets
        // ***************************************************
        if (lastPacketLength == MAX_PACKET_SIZE)
        {
            int packetLength;
            do
            {
                remaining = 4;
                off = 0;
                do
                {
                    var count = await _stream.ReadAsync(_header, off, remaining, cancellationToken);
                    if (count < 0) throw new IOException("unexpected end of stream, read " + off + " bytes from 4");
                    remaining -= count;
                    off += count;
                } while (remaining > 0);

                packetLength = (_header[0] & 0xff) + ((_header[1] & 0xff) << 8) + ((_header[2] & 0xff) << 16);

                remaining = packetLength;
                // skipping 
                do
                {
                    var count = await _stream.ReadAsync(_header, 0, Math.Min(4, remaining), cancellationToken);
                    if (count < 0)
                        throw new IOException(
                            "unexpected end of stream, skipping bytes (socket was closed by server)");
                    remaining -= count;
                    off += count;
                } while (remaining > 0);

                lastPacketLength += packetLength;
            } while (packetLength == MAX_PACKET_SIZE);
        }
    }

    public MutableByte GetSequence()
    {
        return _sequence;
    }

    public void Close()
    {
        _stream.Close();
    }

    public void SetServerThreadId(long? serverThreadId, HostAddress hostAddress)
    {
        var isMaster = hostAddress?.Primary;
        _serverThreadLog =
            "conn="
            + (serverThreadId == null ? "-1" : serverThreadId)
            + (isMaster != null ? " (" + (isMaster.Value ? "M" : "S") + ")" : "");
    }
}