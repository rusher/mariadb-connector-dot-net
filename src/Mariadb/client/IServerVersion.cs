namespace Mariadb.client;

public interface IServerVersion
{
    public bool VersionGreaterOrEqual(int major, int minor, int patch);
}