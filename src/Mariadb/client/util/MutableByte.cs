namespace Mariadb.client.util;

public class MutableByte
{
    private byte _value = 0xff;

    public byte Value
    {
        get => _value;
        set => _value = value;
    }

    public byte incrementAndGet() {
        return ++_value;
    }
}