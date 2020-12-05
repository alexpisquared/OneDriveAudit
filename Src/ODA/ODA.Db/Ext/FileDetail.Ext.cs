namespace Db.OneDriveAudit.DbModel
{
  using System.ComponentModel.DataAnnotations.Schema;
  using System.IO;

  public partial class FileDetail
  {
    [NotMapped]
    public string FullPath { get => Path.Combine(FilePath, FileName); }
  }
}
