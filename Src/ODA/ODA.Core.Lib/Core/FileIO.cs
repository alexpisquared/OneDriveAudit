using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ODA.Core.Lib.Core
{
  public class FileIO
  {
    public static Task<string[]> getFilesAsync(string folder) { return Task.Run(() => Directory.GetFiles(folder, "*", SearchOption.AllDirectories)); }
    public static Task<FileInfo[]> getFileInfosAsync(string folder) { return Task.Run(() => new DirectoryInfo(folder).GetFiles("*.*", SearchOption.AllDirectories)); }




    static string GetMd5Hash(HashAlgorithm md5Hash, string input)
    {
      byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

      StringBuilder sBuilder = new StringBuilder();

      for (int i = 0; i < data.Length; i++)
      {
        sBuilder.Append(data[i].ToString("x2"));
      }

      return sBuilder.ToString();
    }


  }


}
