using AAV.Db.EF;
using AAV.Sys.Helpers;
using Db.OneDriveAudit.DbModel;
using MVVM.Common;
using System;
using System.Threading.Tasks;

namespace ODA.VM.VM
{
  public partial class MainVM : BindableBaseViewModel
  {
    async Task F1_() { IsReady = false; Bpr.BeepClk(); await Task.Delay(9); await fastSyncFStoDbSansHashing_Folder(CurrentFolder, true); IsReady = true; }
    async Task F2_() { IsReady = false; Bpr.BeepClk(); await Task.Delay(9); IsReady = true; }
    async Task F3_() { IsReady = false; Bpr.BeepClk(); await findDupesInDbAndDeleteThemFromFS_SUSPENDED(CurrentFolder, -1, "DoubleFolder", -2, MinFileSize); IsReady = true; }
    async Task F4_() { IsReady = false; Bpr.BeepClk(); await Task.Delay(9); IsReady = true; }
    async Task F5_() { IsReady = false; Bpr.BeepClk(); await Task.Delay(9); IsReady = true; }
    async Task F6_() { IsReady = false; Bpr.BeepClk(); await Task.Delay(9); IsReady = true; }
    async Task F7_() { IsReady = false; Bpr.BeepClk(); await UpdateDBfromFS(9); IsReady = true; }
    async Task F8_()
    {
      Bpr.BeepClk(); IsReady = false; await Task.Delay(9);
      using (var db = new A0DbContext())
      {
        db.FileDetails.RemoveRange(db.FileDetails);
        SaveRpt += Environment.NewLine + db.GetDbChangesReport();
      }
      IsReady = true;
    }
    async Task F9_()
    {
      await F1_();
      await F2_();
      await F3_();

      synth.Speak("All and everithing is Done.");
    }
    async Task FA_() { Bpr.BeepClk(); IsReady = false; await DeleteAllDupesAutoAndManual_F10(); IsReady = true; }
  }
}
