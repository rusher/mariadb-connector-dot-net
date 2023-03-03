namespace Mariadb;

public class HostAddress
{

  public string Host;
  public uint Port;
  public bool? Primary;

  private HostAddress(string host, uint port, bool? primary) {
    Host = host;
    Port = port;
    Primary = primary;
  }

  public static HostAddress from(string host, uint port) {
    return new HostAddress(host, port, null);
  }

  public static HostAddress from(string host, uint port, bool? primary) {
    return new HostAddress(host, port, primary);
  }

  public static List<HostAddress> parse(string spec, HaMode haMode) {
    if ("".Equals(spec)) {
      return new List<HostAddress>();
    }
    string[] tokens = spec.Trim().Split(",");
    int size = tokens.Length;
    List<HostAddress> arr = new List<HostAddress>(size);

    for (int i = 0; i < tokens.Length; i++) {
      String token = tokens[i];
      if (token.StartsWith("address=")) {
        arr.Add(parseParameterHostAddress(token, haMode, i == 0));
      } else {
        arr.Add(parseSimpleHostAddress(token, haMode, i == 0));
      }
    }

    return arr;
  }

  private static HostAddress parseSimpleHostAddress(String str, HaMode haMode, bool first) 
  {
    string host;
    uint port = 3306;

    if (str[0] == '[') {
      /* IPv6 addresses in URLs are enclosed in square brackets */
      int ind = str.IndexOf(']');
      host = str.Substring(1, ind);
      if (ind != (str.Length - 1) && str[ind + 1] == ':') {
        port = getPort(str.Substring(ind + 2));
      }
    } else if (str.Contains(":")) {
      /* Parse host:port */
      String[] hostPort = str.Split(":");
      host = hostPort[0];
      port = getPort(hostPort[1]);
    } else {
      /* Just host name is given */
      host = str;
    }

    bool primary = haMode != HaMode.REPLICATION || first;

    return new HostAddress(host, port, primary);
  }

  private static uint getPort(string portString)
  {
    if (UInt32.TryParse(portString, out uint port)) return port;
    throw new ArgumentException($"Port has wrong int32 value '{portString}'.");
  }

  private static HostAddress parseParameterHostAddress(string str, HaMode haMode, bool first)
  {
    string host = null;
    uint port = 3306;
    bool? primary = null;

    String[] array = str.Replace(" ", "").Split("(?=\\()|(?<=\\))");
    for (int i = 1; i < array.Length; i++) {
      String[] token = array[i].Replace("(", "").Replace(")", "").Trim().Split("=");
      if (token.Length != 2)
      {
        throw new ArgumentException(
          $"Invalid connection URL, expected key=value pairs, found '{array[i]}'.");
      }
      String key = token[0].ToLowerInvariant();
      String value = token[1].ToLowerInvariant();

      switch (key) {
        case "host":
          host = value.Replace("[", "").Replace("]", "");
          break;
        case "port":
          port = getPort(value);
          break;
        case "type":
          if ("master".Equals(value.ToLowerInvariant()) || "primary".Equals(value.ToLowerInvariant())) {
            primary = true;
          } else if ("slave".Equals(value.ToLowerInvariant()) || "replica".Equals(value.ToLowerInvariant())) {
            primary = false;
          } else {
            throw new ArgumentException(
              $"Invalid type value '{array[i]}' (possible value primary/replica).");
          }
          break;
      }
    }

    if (primary == null) {
      if (haMode == HaMode.REPLICATION) {
        primary = first;
      } else {
        primary = true;
      }
    }

    return new HostAddress(host, port, primary);
  }

  public override string ToString()
  {
    return String.Format(
        "address=(host={0})(port={1}){2}",
        Host, Port, ((Primary != null) ? ("(type=" + (Primary == true ? "primary)" : "replica)")) : ""));
  }

}