using Mariadb.client;
using Mariadb.client.socket;
using Mariadb.client.util;

namespace Mariadb.plugin.authentication;

public interface IAuthenticationPlugin
{
    string Type { get; }
    void Initialize(string authenticationData, byte[] seed, Configuration conf);
    Task<IReadableByteBuf> Process(CancellationToken token, IWriter writer, IReader reader, IContext context);
}