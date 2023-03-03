using System.Net.Sockets;
using Mariadb.client.impl;
using Mariadb.client.util;
using Mariadb.utils.log;

namespace Mariadb.client.socket;

public class PacketReader : IReader
{
    private const int REUSABLE_BUFFER_LENGTH = 1024;
    private const int MAX_PACKET_SIZE = 0xffffff;
    private static readonly Ilogger logger = Loggers.getLogger("PacketReader");
    private readonly NetworkStream _in;
    private readonly uint _maxQuerySizeToLog;

    private readonly MutableByte _sequence;
    private string _serverThreadLog = "";

    private readonly byte[] header = new byte[4];

    private readonly IReadableByteBuf readBuf = new StandardReadableByteBuf(null, 0);
    private readonly byte[] reusableArray = new byte[REUSABLE_BUFFER_LENGTH];

    public PacketReader(NetworkStream networkStream, Configuration conf, MutableByte sequence)
    {
        _in = networkStream;
        _maxQuerySizeToLog = conf.MaxQuerySizeToLog;
        _sequence = sequence;
    }

    public IReadableByteBuf ReadableBufFromArray(byte[] buf)
    {
        readBuf.Buf(buf, buf.Length, 0);
        return readBuf;
    }

    public IReadableByteBuf ReadReusablePacket()
    {
        return ReadReusablePacket(logger.isTraceEnabled());
    }

    public IReadableByteBuf ReadReusablePacket(bool traceEnable)
    {
        // ***************************************************
        // Read 4 byte header
        // ***************************************************
        var remaining = 4;
        var off = 0;
        do
        {
            var count = _in.Read(header, off, remaining);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + off
                    + " bytes from 4 (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        var lastPacketLength =
            (header[0] & 0xff) + ((header[1] & 0xff) << 8) + ((header[2] & 0xff) << 16);
        _sequence.Value = header[3];

        // prepare array
        byte[] rawBytes;
        if (lastPacketLength < REUSABLE_BUFFER_LENGTH)
            rawBytes = reusableArray;
        else
            rawBytes = new byte[lastPacketLength];

        // ***************************************************
        // Read content
        // ***************************************************
        remaining = lastPacketLength;
        off = 0;
        do
        {
            var count = _in.Read(rawBytes, off, remaining);
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
                $"read: {_serverThreadLog}\n{LoggerHelper.Hex(header, rawBytes, 0, lastPacketLength, _maxQuerySizeToLog)}");

        readBuf.Buf(rawBytes, lastPacketLength, 0);
        return readBuf;
    }

    public byte[] ReadPacket(bool traceEnable)
    {
        // ***************************************************
        // Read 4 byte header
        // ***************************************************
        var remaining = 4;
        var off = 0;
        do
        {
            var count = _in.Read(header, off, remaining);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + off
                    + " bytes from 4 (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        var lastPacketLength =
            (header[0] & 0xff) + ((header[1] & 0xff) << 8) + ((header[2] & 0xff) << 16);

        // prepare array
        var rawBytes = new byte[lastPacketLength];

        // ***************************************************
        // Read content
        // ***************************************************
        remaining = lastPacketLength;
        off = 0;
        do
        {
            var count = _in.Read(rawBytes, off, remaining);
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
                $"read: {_serverThreadLog}\n{LoggerHelper.Hex(header, rawBytes, 0, lastPacketLength, _maxQuerySizeToLog)}");

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
                    var count = _in.Read(header, off, remaining);
                    if (count < 0) throw new IOException("unexpected end of stream, read " + off + " bytes from 4");
                    remaining -= count;
                    off += count;
                } while (remaining > 0);

                packetLength = (header[0] & 0xff) + ((header[1] & 0xff) << 8) + ((header[2] & 0xff) << 16);

                var currentbufLength = rawBytes.Length;
                var newRawBytes = new byte[currentbufLength + packetLength];
                Array.Copy(rawBytes, 0, newRawBytes, 0, currentbufLength);
                rawBytes = newRawBytes;

                // ***************************************************
                // Read content
                // ***************************************************
                remaining = packetLength;
                off = currentbufLength;
                do
                {
                    var count = _in.Read(rawBytes, off, remaining);
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
                        $"read: {_serverThreadLog}\n{LoggerHelper.Hex(header, rawBytes, currentbufLength, packetLength, _maxQuerySizeToLog)}");

                lastPacketLength += packetLength;
            } while (packetLength == MAX_PACKET_SIZE);
        }

        return rawBytes;
    }

    public void SkipPacket()
    {
        if (logger.isTraceEnabled())
        {
            ReadReusablePacket(logger.isTraceEnabled());
            return;
        }

        // ***************************************************
        // Read 4 byte header
        // ***************************************************
        var remaining = 4;
        var off = 0;
        do
        {
            var count = _in.Read(header, off, remaining);
            if (count < 0)
                throw new IOException(
                    "unexpected end of stream, read "
                    + off
                    + " bytes from 4 (socket was closed by server)");
            remaining -= count;
            off += count;
        } while (remaining > 0);

        var lastPacketLength =
            (header[0] & 0xff) + ((header[1] & 0xff) << 8) + ((header[2] & 0xff) << 16);

        remaining = lastPacketLength;
        // skipping 
        do
        {
            var count = _in.Read(header, 0, Math.Min(4, remaining));
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
                    var count = _in.Read(header, off, remaining);
                    if (count < 0) throw new IOException("unexpected end of stream, read " + off + " bytes from 4");
                    remaining -= count;
                    off += count;
                } while (remaining > 0);

                packetLength = (header[0] & 0xff) + ((header[1] & 0xff) << 8) + ((header[2] & 0xff) << 16);

                remaining = packetLength;
                // skipping 
                do
                {
                    var count = _in.Read(header, 0, Math.Min(4, remaining));
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
        _in.Close();
    }

    public void SetServerThreadId(long serverThreadId, HostAddress hostAddress)
    {
        var isMaster = hostAddress?.Primary;
        _serverThreadLog =
            "conn="
            + (serverThreadId == null ? "-1" : serverThreadId)
            + (isMaster != null ? " (" + (isMaster.Value ? "M" : "S") + ")" : "");
    }
}