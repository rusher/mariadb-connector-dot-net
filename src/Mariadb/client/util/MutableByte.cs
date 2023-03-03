namespace Mariadb.client.util;

public class MutableByte
{
    public byte Value { get; set; } = 0xff;

    public byte incrementAndGet()
    {
        return ++Value;
    }
}