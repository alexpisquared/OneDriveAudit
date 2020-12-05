using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ODA.View.View
{
  public partial class DeleteDblChker : Window
  {
    bool _isPlaying = false;
    readonly string _dupe, _main;

    public DeleteDblChker(string fDupe, string fMain, string hash1, string hash2, long len1, long len2)
    {
      InitializeComponent();

      _dupe = fDupe;
      _main = fMain;

      MouseLeftButtonDown += (s, e) => DragMove();
      KeyDown += (s, ves) =>
      {
        switch (ves.Key)
        {
          case Key.Home: me1.Position = me2.Position = TimeSpan.FromTicks(0); break;
          case Key.Space: if (_isPlaying) { me1.Pause(); me2.Pause(); } else { me1.Play(); me2.Play(); } _isPlaying = !_isPlaying; break;
          case Key.Escape: Close(); /*App.Current.Shutdown(); */ break;
        }
      };

      me1.Source = new Uri(fDupe);
      me2.Source = new Uri(fMain);

      tb1.Text = tB1.Text = fDupe;
      tb2.Text = tB2.Text = fMain;

      th1.Text = hash1;
      th2.Text = hash2;

      tl1.Text = len1.ToString("N0");
      tl2.Text = len2.ToString("N0");
    }

    void onEnd(object sender, RoutedEventArgs e) { cbYN.IsChecked = null; me1.Stop(); Close(); }
    void onYes(object sender, RoutedEventArgs e) { cbYN.IsChecked = true; me1.Stop(); Close(); }
    void onNoo(object sender, RoutedEventArgs e) { cbYN.IsChecked = false; me1.Stop(); Close(); }

    void me1_Loaded(object sender, RoutedEventArgs e) { if (me2.IsLoaded) { me1.Play(); me2.Play(); _isPlaying = true; } }
    void me2_Loaded(object sender, RoutedEventArgs e) { if (me1.IsLoaded) { me1.Play(); me2.Play(); _isPlaying = true; } }

    void onExp(object sender, RoutedEventArgs e)
    {
      Process.Start("Explorer.exe", Path.GetDirectoryName(_dupe));
      Process.Start("Explorer.exe", Path.GetDirectoryName(_main));
    }
  }
}
