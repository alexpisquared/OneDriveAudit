using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace ODA.View.View
{
    public partial class MainControlPanel : Window
    {
        public MainControlPanel()
        {
            InitializeComponent();
            MouseLeftButtonDown += (s, e) => DragMove(); KeyDown += (s, ves) => { switch (ves.Key) { case Key.Escape: Close(); /*App.Current.Shutdown(); */ break; } };

#if DEBUG
            var compileMode = "Dbg";
#else
      var compileMode = "Rls";
#endif

            Title += $"{Environment.MachineName} - {compileMode}";

            tbBuildTime.Header =
                      (DateTime.Now - new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime).TotalHours < 48 ?
                        $"{(DateTime.Now - new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime).TotalHours:N1} hr ago  {compileMode}"
                : $"{new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime:yyyy.MM.dd}    {compileMode}";

            Top = Environment.MachineName == "ASUS2" ? 103 : 400;
        }
        void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)); e.Handled = true; }
    }
}