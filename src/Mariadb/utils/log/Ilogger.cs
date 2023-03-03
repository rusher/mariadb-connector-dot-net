namespace Mariadb.utils.log;

public interface Ilogger
{
  string getName();
  bool isTraceEnabled();
  void trace(string msg);
  void trace(String msg, Exception e);
  bool isDebugEnabled();

  void debug(string msg);
  void debug(string msg, Exception e);

  bool isInfoEnabled();
  void info(String msg);
  void info(String msg, Exception e);

  bool isWarnEnabled();
  void warn(String msg);
  void warn(String msg, Exception e);

  bool isErrorEnabled();
  void error(String msg);
  void error(String msg, Exception e);
}