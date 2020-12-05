using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODA.ConsoleApp
{
  public class PocBits
  {
    public void M1()
    {

      var path = @"C:\Users\alexp\OneDrive\Pictures\Camera Roll 1\2020";

      var fis = new DirectoryInfo(path).GetFiles("*.*", SearchOption.AllDirectories);

      Console.WriteLine($"{fis.Length,6:N0}  files ");
      Console.WriteLine($"");


      var gis = fis.GroupBy(
        p => p.Length,
        p => p.Name,
        (key, g) => new { Length = key, FNames = g.ToList() });

      Console.WriteLine($"{gis.ToList().Count,6:N0}  files ");

      foreach (var g in gis.Where(r => r.FNames.Count > 1).OrderByDescending(r => r.FNames.Count).OrderByDescending(r => r.Length))
      {
        Console.WriteLine($"\n{g.Length,12:N0}   {g.FNames.Count,12:N0}:");
        foreach (var f in g.FNames.OrderBy(n => n))
        {
          Console.WriteLine($"             {f}");
        }
      }


    }
    public Task<FileInfo[]> getFileInfosAsync(string folder, string searchPattern = "*.*") => Task.Run(() => new DirectoryInfo(folder).GetFiles(searchPattern, SearchOption.AllDirectories));
  }
}
