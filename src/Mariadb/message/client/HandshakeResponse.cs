using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Mariadb.client;
using Mariadb.client.socket;
using Mariadb.plugin.authentication.standard;
using Mariadb.utils.constant;

namespace Mariadb.message.client;

public class HandshakeResponse : AbstractClientMessage
{
    private static readonly string _CLIENT_NAME = "_client_name";
    private static readonly string _CLIENT_VERSION = "_client_version";
    private static readonly string _SERVER_HOST = "_server_host";
    private static readonly string _OS = "_os";
    private static readonly string _THREAD = "_thread";
    private static string _JAVA_VENDOR = "_java_vendor";
    private static string _JAVA_VERSION = "_java_version";
    private readonly ulong _clientCapabilities;
    private readonly string _connectionAttributes;
    private readonly string _database;
    private readonly byte _exchangeCharset;
    private readonly string _host;
    private readonly string _password;
    private readonly byte[] _seed;

    private readonly string _username;
    private string _authenticationPluginType;

    public HandshakeResponse(
        string username,
        string password,
        string authenticationPluginType,
        byte[] seed,
        Configuration conf,
        string host,
        ulong clientCapabilities,
        byte exchangeCharset)
    {
        _authenticationPluginType = authenticationPluginType;
        _seed = seed;
        _username = username;
        _password = password;
        _database = conf.Database;
        _connectionAttributes = conf.ConnectionAttributes;
        _host = host;
        _clientCapabilities = clientCapabilities;
        _exchangeCharset = exchangeCharset;
    }

    public override string Description => "HandshakeResponse";

    private static void WriteStringLengthAscii(IWriter encoder, string value)
    {
        var valBytes = Encoding.ASCII.GetBytes(value);
        encoder.WriteLength(valBytes.Length);
        encoder.WriteBytes(valBytes);
    }

    private static void WriteStringLength(IWriter encoder, string value)
    {
        var valBytes = Encoding.UTF8.GetBytes(value);
        encoder.WriteLength(valBytes.Length);
        encoder.WriteBytes(valBytes);
    }

    private static async Task WriteConnectAttributes(
        IWriter writer, string connectionAttributes, string host)
    {
        var tmpWriter = new PacketWriter(null, 0, 0, null, null);
        tmpWriter.Pos = 0;
        WriteStringLengthAscii(tmpWriter, _CLIENT_NAME);
        WriteStringLength(tmpWriter, "MariaDB dot.net");

        WriteStringLengthAscii(tmpWriter, _CLIENT_VERSION);
        WriteStringLength(tmpWriter, "0.0.1");

        WriteStringLengthAscii(tmpWriter, _SERVER_HOST);
        WriteStringLength(tmpWriter, host != null ? host : "");

        WriteStringLengthAscii(tmpWriter, _OS);
        WriteStringLength(tmpWriter, RuntimeInformation.OSDescription);

        WriteStringLengthAscii(tmpWriter, _THREAD);
        WriteStringLength(tmpWriter, Process.GetCurrentProcess().Id.ToString());

        if (connectionAttributes != null)
        {
            var tokenizer = connectionAttributes.Split(",");
            foreach (var token in tokenizer)
            {
                var separator = token.IndexOf(":");
                if (separator != -1)
                {
                    WriteStringLength(tmpWriter, token.Substring(0, separator));
                    WriteStringLength(tmpWriter, token.Substring(separator + 1));
                }
                else
                {
                    WriteStringLength(tmpWriter, token);
                    WriteStringLength(tmpWriter, "");
                }
            }
        }

        await writer.WriteLength(tmpWriter.Pos);
        await writer.WriteBytes(tmpWriter.Buf, 0, tmpWriter.Pos);
    }

    public override async Task<int> Encode(CancellationToken cancellationToken, IWriter writer, IContext context)
    {
        byte[] authData;
        if (string.Equals("mysql_clear_password", _authenticationPluginType))
        {
            if (!context.HasClientCapability(Capabilities.SSL))
                throw new ArgumentException("Cannot send password in clear if SSL is not enabled.");
            authData =
                _password == null ? new byte[0] : Encoding.UTF8.GetBytes(_password);
        }
        else
        {
            _authenticationPluginType = "mysql_native_password";
            authData = NativePasswordPlugin.encryptPassword(_password, _seed);
        }

        await writer.WriteInt((int)_clientCapabilities);
        await writer.WriteInt(1024 * 1024 * 1024);
        await writer.WriteByte(_exchangeCharset); // 1

        await writer.WriteBytes(new byte[19]); // 19
        await writer.WriteInt((int)(_clientCapabilities >> 32)); // Maria extended flag

        await writer.WriteString(_username != null ? _username : WindowsIdentity.GetCurrent().Name);
        await writer.WriteByte(0x00);

        if (context.HasServerCapability(Capabilities.PLUGIN_AUTH_LENENC_CLIENT_DATA))
        {
            await writer.WriteLength(authData.Length);
            await writer.WriteBytes(authData);
        }
        else if (context.HasServerCapability(Capabilities.SECURE_CONNECTION))
        {
            await writer.WriteByte((byte)authData.Length);
            await writer.WriteBytes(authData);
        }
        else
        {
            await writer.WriteBytes(authData);
            await writer.WriteByte(0x00);
        }

        if (context.HasServerCapability(Capabilities.CONNECT_WITH_DB))
        {
            await writer.WriteString(_database);
            await writer.WriteByte(0x00);
        }

        if (context.HasServerCapability(Capabilities.PLUGIN_AUTH))
        {
            await writer.WriteString(_authenticationPluginType);
            await writer.WriteByte(0x00);
        }

        if (context.HasServerCapability(Capabilities.CONNECT_ATTRS))
            await WriteConnectAttributes(writer, _connectionAttributes, _host);
        //writer.flush();
        return 1;
    }
}