using AsLink;
using MVVM.Common;
using ODA.App.Interfaces;
using ODA.View.View;
using ODA.VM.VM;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ODA.App
{
  public partial class App : Application
  {
    public Stopwatch SW = Stopwatch.StartNew();

    protected override void OnStartup(StartupEventArgs e)
    {
      //Bpr.BeepFD(10000, 30);
      Current.DispatcherUnhandledException += DevOpStartup.OnCurrentDispatcherUnhandledException;
      EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, re) => { (s as TextBox).SelectAll(); })); //tu: TextBox
      AAV.Sys.Helpers.Tracer.SetupTracingOptions("ODA", new TraceSwitch("Verbose________Trace", "This is the trace for all               messages.") { Level = TraceLevel.Info });
      Trace.WriteLine($"*{DateTime.Now:MMdd HH:mm:ss}  CmdLn: [{Environment.CommandLine}]");

      base.OnStartup(e);

      BindableBaseViewModel.ShowModalMvvm(new MainVM(new CreateNewUenWindowFactory()), new MainControlPanel());

      App.Current.Shutdown();
    }
    protected override void OnExit(ExitEventArgs e) => base.OnExit(e); //			Trace.WriteLine(string.Format("*{0:MMdd HH:mm:ss} The End. Took {1:hh\\:mm\\:ss}.", DateTime.Now, SW.Elapsed));
  }

  public class CreateNewUenWindowFactory : IMvvmVmWindowFactory
  {
    public void ShowNewWindow(INotifyPropertyChanged vm, string file1, string file2, string hash1, string hash2, long len1, long len2) => BindableBaseViewModel.ShowModalMvvm((BindableBaseViewModel)vm, new DeleteDblChker(file1, file2, hash1, hash2, len1, len2));
  }

}

/// 2018-12-31:
/// Was unable to auto init the db: restored from ASUS2 and ran the app on top of it ...still got the spatio types error.
/// todo: delete and run clean dbini off this pc's onedrive as is.