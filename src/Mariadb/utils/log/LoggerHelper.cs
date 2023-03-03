using System.Text;

namespace Mariadb.utils.log;

public class LoggerHelper
{
   
  private static char[] hexArray = "0123456789ABCDEF".ToCharArray();

  public static string Hex(byte[] bytes, int offset, int dataLength, uint trunkLength= Int32.MaxValue) {

    if (bytes == null || bytes.Length == 0) {
      return "";
    }

    char[] hexaValue = new char[16];
    hexaValue[8] = ' ';

    int pos = offset;
    int line = 1;
    int posHexa = 0;
    int logLength = Math.Min(dataLength, (int)trunkLength);
    StringBuilder sb = new StringBuilder(logLength * 3);
    sb.Append(
        "       +--------------------------------------------------+\n"
            + "       |  0  1  2  3  4  5  6  7   8  9  a  b  c  d  e  f |\n"
            + "+------+--------------------------------------------------+------------------+\n|000000| ");

    while (pos < logLength + offset) {
      int byteValue = bytes[pos] & 0xFF;
      sb.Append(hexArray[byteValue >>> 4]).Append(hexArray[byteValue & 0x0F]).Append(" ");

      hexaValue[posHexa++] = (byteValue > 31 && byteValue < 127) ? (char) byteValue : '.';

      if (posHexa == 8) {
        sb.Append(" ");
      }
      if (posHexa == 16) {
        sb.Append("| ").Append(hexaValue).Append(" |\n");
        if (pos + 1 != logLength + offset)
          sb.Append("|").Append(MediumIntToHex(line++)).Append("| ");
        posHexa = 0;
      }
      pos++;
    }

    int remaining = posHexa;
    if (remaining > 0) {
      if (remaining < 8) {
        for (; remaining < 8; remaining++) {
          sb.Append("   ");
        }
        sb.Append(" ");
      }

      for (; remaining < 16; remaining++) {
        sb.Append("   ");
      }

      for (; posHexa < 16; posHexa++) {
        hexaValue[posHexa] = ' ';
      }

      sb.Append("| ").Append(hexaValue).Append(" |\n");
    }
    if (dataLength > trunkLength) {
      sb.Append("+------+-------------------truncated----------------------+------------------+\n");
    } else {
      sb.Append("+------+--------------------------------------------------+------------------+\n");
    }
    return sb.ToString();
  }

  private static string MediumIntToHex(int value) {
    string st = (value*16).ToString("X");
    while (st.Length < 6) st = "0" + st;
    return st;
  }

  public static string Hex(
      byte[] header, byte[] bytes, int offset, int dataLength, uint trunkLength = Int32.MaxValue) {
    byte[] complete = new byte[dataLength + header.Length];
    Array.Copy(header, 0, complete, 0, header.Length);
    Array.Copy(bytes, offset, complete, header.Length, dataLength);
    return Hex(complete, 0, dataLength + header.Length, trunkLength);
  }
}