namespace Mariadb.utils.log;

public class ConsoleLogger : Ilogger
{
    private static readonly TextWriter _err = Console.Error;
    private static readonly TextWriter _log = Console.Out;
    private readonly bool _logDebugLvl;

    private readonly string _name;

    public ConsoleLogger(string name, bool logDebugLvl)
    {
        _name = name;
        _logDebugLvl = logDebugLvl;
    }

    public string getName()
    {
        return _name;
    }

    public bool isTraceEnabled()
    {
        return _logDebugLvl;
    }

    public void trace(string msg)
    {
        _log.Write(msg);
    }

    public void trace(string msg, Exception e)
    {
        _log.Write(msg, e);
    }

    public bool isDebugEnabled()
    {
        return _logDebugLvl;
    }

    public void debug(string msg)
    {
        _log.Write(msg);
    }

    public void debug(string msg, Exception e)
    {
        _log.Write(msg, e);
    }

    public bool isInfoEnabled()
    {
        return _logDebugLvl;
    }

    public void info(string msg)
    {
        _log.Write(msg);
    }

    public void info(string msg, Exception e)
    {
        _log.Write(msg, e);
    }

    public bool isWarnEnabled()
    {
        return _logDebugLvl;
    }

    public void warn(string msg)
    {
        _log.Write(msg);
    }

    public void warn(string msg, Exception e)
    {
        _log.Write(msg, e);
    }

    public bool isErrorEnabled()
    {
        return true;
    }

    public void error(string msg)
    {
        _err.Write(msg);
    }

    public void error(string msg, Exception e)
    {
        _err.Write(msg, e);
    }
}