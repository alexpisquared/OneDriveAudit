using AAV.Sys.Ext;
using AAV.Sys.Helpers;
using MVVM.Common;
using ODA.App.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Speech.Synthesis;
using System.Windows.Input;
using System.Windows.Shell;

namespace ODA.VM.VM
{
  public partial class MainVM : BindableBaseViewModel
  {
    const string _nfo = "<Not Found on ", _ndm = "<Not Deleting - rejected by manual unmatch to", _ndsne = "<Not Deleting - since one does not exists {0}>\r\n";
    readonly SpeechSynthesizer synth = new SpeechSynthesizer();
    readonly IMvvmVmWindowFactory _newUenWindowF;
    string _MasterSignature, _DoubleSignature;

    public MainVM(IMvvmVmWindowFactory newUenWindowFactory) => _newUenWindowF = newUenWindowFactory;
    protected override void AutoExec()
    {
      base.AutoExec();
      Bpr.Beep1of2();

      try
      {
        //Task.Run(CountDown(15.1)).ContinueWith(_ => { if (CanStopCntDn) { CanStopCntDn = false; onF9All4Steps(null); } });

#if DEBUG
        //CurrentFolder = @"C:\temp\oda\Pictures\";

        //DoubleFolder = @"C:\temp\oda\Pictures\Camera Roll~\";
        //DoubleFolder = @"\\ln1\1\Pic\";
        //DoubleFolder = @"\\ln1\1\Pic.DevDbg\";
        //DoubleFolder = @"C:\Users\alex.pigida.ABSCIEXDEV\Videos\Captures\";
#else
        //DoubleFolder =
        //	Environment.MachineName == "ASUS2" ? OneDrive.Folder(@"Pictures\Camera Roll~\") : //C:\temp\dbg.2015-11-27 - 2016-01-24 L950XL.dbg\") : //          @"\\LN1\Pictures\Camera Roll~\" 
        //	Environment.MachineName == "LN1" ? @"C:\1\Pic\" :
        //	OneDrive.Folder(@"C:\Users\jingm\OneDrive\Pictures\Camera Roll\");
#endif

        CurrentFolder = OneDrive.Folder(@"Pictures\");
        CamRollFolder = OneDrive.Folder(@"Pictures\Camera Roll 1\");

        if (Environment.MachineName == "NUC1")
          AllDirs = new ObservableCollection<string>(new[] {
            CurrentFolder,
            @"\\LN1\1\Pic\",
            @"\\LN1\Pictures.JM1d\", // former @"\\LN1\Users\jingm\OneDrive\Pictures\Camera Roll\" ... but was not available at the moment of loading, thus switched to Pictures.JM1d ..so keep it this way lest unnesessary doubling db entries.
          });
        else if (Environment.MachineName == "ASUS2")
          AllDirs = new ObservableCollection<string>(new[] {
            CurrentFolder,
            @"C:\temp\oda\Pictures\",
            @"C:\temp\oda\Pictures\Camera Roll~\"
          });
        else if (Environment.MachineName == "DESKTOP-O082HP4")
          AllDirs = new ObservableCollection<string>(new[] {
            @"C:\Users\alex.pigida.ABSCIEXDEV\Videos\Captures",
          });
        else
          AllDirs = new ObservableCollection<string>(new[] {
            @"C:\Users\alex.pigida.ABSCIEXDEV\Videos\Captures",
          });


        _MasterSignature = @"\Pictures\";
        _DoubleSignature = @"\Pictures\Camera Rol";


        //autoAll();

#if __DEBUG
        Task.Run(async () => await F1_()).Wait();
#endif
      }
      catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }

      Bpr.Beep2of2();
    }


    protected override bool CanClose() => true;  // !CanStopCntDn; }

    bool _IsReady = true;       /**/ public bool IsReady { get => _IsReady; set { if (Set(ref _IsReady, value)) MUProgressState = value ? TaskbarItemProgressState.None : TaskbarItemProgressState.Normal; } }    //bool _IsReady = true;   /**/ public bool IsBusy { get { return _IsBusy; } set { Set(ref _IsBusy, value); } }
    string _CurrentFolder;      /**/ public string CurrentFolder { get => _CurrentFolder; set => Set(ref _CurrentFolder, value); }
    string _CamRollFolder;      /**/ public string CamRollFolder { get => _CamRollFolder; set => Set(ref _CamRollFolder, value); }
    int _MinFileSize;           /**/ public int MinFileSize { get => _MinFileSize; set => Set(ref _MinFileSize, value); }
    string _InfoMsg;            /**/ public string InfoMsg { get => _InfoMsg; set => Set(ref _InfoMsg, value); }
    string _FHashed;            /**/ public string FHashed { get => _FHashed; set => Set(ref _FHashed, value); }
    string _SaveRpt;            /**/ public string SaveRpt { get => _SaveRpt; set => Set(ref _SaveRpt, value); }
    string _ErrorMsg;           /**/ public string ErrorMsg { get => _ErrorMsg; set => Set(ref _ErrorMsg, value); }
    long _ttlLen = 100;         /**/ public long TtlLen { get => _ttlLen; set => Set(ref _ttlLen, value); }
    long _prgLen = 0;           /**/ public long PrgLen { get => _prgLen; set { if (Set(ref _prgLen, value)) MUProgressPerc = TtlLen == 0 ? 0d : (double)PrgLen / TtlLen; } }
    double _MUProgressPerc = 0; /**/ public double MUProgressPerc { get => _MUProgressPerc; set => Set(ref _MUProgressPerc, value); }
    TaskbarItemProgressState _MUProgressState = TaskbarItemProgressState.Normal; public TaskbarItemProgressState MUProgressState { get => _MUProgressState; set => Set(ref _MUProgressState, value); }
    ObservableCollection<string> _allDirs = new ObservableCollection<string>(); public ObservableCollection<string> AllDirs { get => _allDirs; set => Set(ref _allDirs, value); }

    ICommand _Go_; public ICommand Go_Cmd => _Go_ ?? (_Go_ = new RelayCommand(async x => await F1_(), x => IsReady) { GestureKey = Key.Enter, GestureModifier = ModifierKeys.None });
    ICommand _F1_; public ICommand F1_Cmd => _F1_ ?? (_F1_ = new RelayCommand(async x => await F1_(), x => IsReady) { GestureKey = Key.F1, GestureModifier = ModifierKeys.None });
    ICommand _F2_; public ICommand F2_Cmd => _F2_ ?? (_F2_ = new RelayCommand(async x => await F2_(), x => IsReady) { GestureKey = Key.F2, GestureModifier = ModifierKeys.None });
    ICommand _F3_; public ICommand F3_Cmd => _F3_ ?? (_F3_ = new RelayCommand(async x => await F3_(), x => IsReady) { GestureKey = Key.F3, GestureModifier = ModifierKeys.None });
    ICommand _F4_; public ICommand F4_Cmd => _F4_ ?? (_F4_ = new RelayCommand(async x => await F4_(), x => IsReady) { GestureKey = Key.F4, GestureModifier = ModifierKeys.None });
    ICommand _F5_; public ICommand F5_Cmd => _F5_ ?? (_F5_ = new RelayCommand(async x => await F5_(), x => IsReady) { GestureKey = Key.F5, GestureModifier = ModifierKeys.None });
    ICommand _F6_; public ICommand F6_Cmd => _F6_ ?? (_F6_ = new RelayCommand(async x => await F6_(), x => IsReady) { GestureKey = Key.F6, GestureModifier = ModifierKeys.None });
    ICommand _F7_; public ICommand F7_Cmd => _F7_ ?? (_F7_ = new RelayCommand(async x => await F7_(), x => IsReady) { GestureKey = Key.F7, GestureModifier = ModifierKeys.None });
    ICommand _F8_; public ICommand F8_Cmd => _F8_ ?? (_F8_ = new RelayCommand(async x => await F8_(), x => IsReady) { GestureKey = Key.F8, GestureModifier = ModifierKeys.None });
    ICommand _F9_; public ICommand F9_Cmd => _F9_ ?? (_F9_ = new RelayCommand(async x => await F9_(), x => IsReady) { GestureKey = Key.F9, GestureModifier = ModifierKeys.None });
    ICommand _FA_; public ICommand FA_Cmd => _FA_ ?? (_FA_ = new RelayCommand(async x => await FA_(), x => IsReady) { GestureKey = Key.F10, GestureModifier = ModifierKeys.None });
    ICommand _FB_; public ICommand FB_Cmd => _FB_ ?? (_FB_ = new RelayCommand(async x => { Bpr.BeepClk(); IsReady = false; await delSameNameSizeFromCameraRoll_F12(CamRollFolder, true); IsReady = true; }, x => IsReady) { GestureKey = Key.F11, GestureModifier = ModifierKeys.None });
    ICommand _FC_; public ICommand FC_Cmd => _FC_ ?? (_FC_ = new RelayCommand(async x => { Bpr.BeepClk(); IsReady = false; await delSameNameSizeFromCameraRoll_F12(CamRollFolder, false); IsReady = true; }, x => IsReady) { GestureKey = Key.F12, GestureModifier = ModifierKeys.None });


    static readonly object _thisLock = new object();
    //private readonly string      _remoteLn1Root = @"\\LN1\",      _remoteNuc1Root = @"\\NUC1\";
  }
}
