using Mariadb.client;
using Version = Mariadb.utils.Version;

namespace Mariadb.message.server.util;

public class ServerVersion : Version, IServerVersion
{
    public ServerVersion(string serverVersion, bool mariaDBServer) : base(serverVersion)
    {
        MariaDBServer = mariaDBServer;
    }

    public bool MariaDBServer { get; }
}