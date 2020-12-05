using System.ComponentModel;

namespace ODA.App.Interfaces
{
  public interface IMvvmVmWindowFactory { void ShowNewWindow(INotifyPropertyChanged vm, string file1, string file2, string hash1, string hash2, long len1, long len2); }
}
