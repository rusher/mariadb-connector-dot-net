namespace Mariadb.utils.log;

public class Loggers
{

  /** factory */
  private static ILoggerFactory LOGGER_FACTORY;

  static Loggers(){
    LOGGER_FACTORY = new ConsoleLoggerFactory();
  }

  public static Ilogger getLogger(String name) {
    return LOGGER_FACTORY.getLogger(name);
  }

  private interface ILoggerFactory {
    Ilogger getLogger(String name);
  }

  private class ConsoleLoggerFactory : ILoggerFactory {

    public Ilogger getLogger(String name) {
      return new ConsoleLogger(name, true);
    }
  }

}