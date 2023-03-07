using System.Security.Cryptography;
using System.Text;
using Mariadb.client;
using Mariadb.client.socket;
using Mariadb.client.util;

namespace Mariadb.plugin.authentication.standard;

public class NativePasswordPlugin : IAuthenticationPlugin
{
    private string _authenticationData;
    private byte[] _seed;

    public string Type => "mysql_native_password";

    public void Initialize(string authenticationData, byte[] seed, Configuration conf)
    {
        _seed = seed;
        _authenticationData = authenticationData;
    }

    public IReadableByteBuf Process(IWriter writer, IReader reader, IContext context)
    {
        if (_authenticationData == null)
        {
            writer.WriteEmptyPacket();
        }
        else
        {
            var truncatedSeed = new byte[_seed.Length - 1];
            Array.Copy(_seed, 0, truncatedSeed, 0, truncatedSeed.Length);
            writer.WriteBytes(encryptPassword(_authenticationData, truncatedSeed));
            writer.Flush();
        }

        return reader.ReadReusablePacket();
    }

    public static byte[] encryptPassword(string password, byte[] seed)
    {
        if (password == null) return new byte[0];
        var sha1 = SHA1.Create();
        var bytePwd = Encoding.UTF8.GetBytes(password);
        Span<byte> stage1 = stackalloc byte[20];
        Span<byte> stage2 = stackalloc byte[20];
        sha1.TryComputeHash(bytePwd, stage1, out _);
        sha1.TryComputeHash(stage1, stage2, out _);


        Span<byte> combined = stackalloc byte[40];
        seed.CopyTo(combined);
        seed.CopyTo(stage2);
        sha1.ComputeHash(seed);

        Span<byte> digest = stackalloc byte[20];
        sha1.TryComputeHash(combined, digest, out _);

        var returnBytes = new byte[digest.Length];
        for (var i = 0; i < digest.Length; i++) returnBytes[i] = (byte)(stage1[i] ^ digest[i]);
        return returnBytes;
    }
}