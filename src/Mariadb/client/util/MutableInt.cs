namespace Mariadb.client.util;

public class MutableInt
{
    private int _value;
    public int Value
    {
        get => _value;
        set => _value = value;
    }

    public int incrementAndGet() {
        return ++_value;
    }
}