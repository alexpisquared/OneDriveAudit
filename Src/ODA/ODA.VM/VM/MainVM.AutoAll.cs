using AAV.Db.EF;
using AAV.Sys.Ext;
using Db.OneDriveAudit.DbModel;
using MVVM.Common;
using ODA.Core.Lib.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ODA.VM.VM
{
  public partial class MainVM : BindableBaseViewModel
  {
#if DEBUG
    const int _dbPackLen = 1000;
#else
    const int _dbPackLen = 10000;
#endif
    async Task fastSyncFStoDbSansHashing_Folder(string folder, bool isMasterish)
    {
      InfoMsg = "F1: ...";

      _TtlBytesDeleted = _TotalAdded = _TotalRenamed = _TotalDeleted = _TotalUpdated = _TotalHashed = 0;
      using (var db = new A0DbContext())
      {
        try
        {
          var sw = Stopwatch.StartNew();

          getAddAuditId(folder, db);

          using (var ha = SHA256Managed.Create()) //_md5 = MD5.Create(),		//  3.5/4.5 faster than SHA256/SHA512			//_sh5 = SHA512Managed.Create(),				//  128 char len					//   64 char len. 3/4 ~ SHA256/SHA512
          {
            var fis = await FileIO.getFileInfosAsync(folder);
            _ah.TotalFound = fis.Count();
            TtlLen = fis.Select(r => r.Length).Sum();
            PrgLen = 0L;
            var idx = 0L;

            synth.SpeakAsync($"{fis.Length} files found. Smart loading them into DB...");

#if ___
#if __
              Parallel.ForEach(fis.ToList(),
#else
            fis.ToList().ForEach(
#endif
              async fi => idx = await doFile(folder, isMasterish, db, sw, ha, idx, fi));
#else
            foreach (var fi in fis)
              idx = await fastSyncFStoDbSansHashing_File(folder, isMasterish, db, sw, ha, idx, fi);
#endif
          }

          SaveRpt += Environment.NewLine + db.GetDbChangesReport(5);
          SaveRpt += Environment.NewLine + await db.TrySaveReportAsync("r/s saved");
        }
        catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }
        finally { updateNewAuditHistRow(_ah.AuditTimeID, "Ini scan into DB"); }
      }

      synth.SpeakAsync("Initial loading to DB is done.");
    }
    async Task<long> fastSyncFStoDbSansHashing_File(string folder, bool isMasterish, A0DbContext db, Stopwatch sw, SHA256 ha, long idx, FileInfo fi)
    {
      var pl = await addOrUpdateToDb_SansHash(folder, db, sw, ha, TtlLen, PrgLen, fi, isMasterish);
      lock (_thisLock)
      {

        PrgLen = pl;
        Interlocked.Increment(ref idx);
        if ((idx) % _dbPackLen == 0) { SaveRpt += Environment.NewLine + db.TrySaveReport($" {idx:N0}").report; }
      }

      return idx;
    }

    async Task HashFolder_OLD(string folder, bool isMasterish)
    {
      InfoMsg = "F1: ...";
      //..synth.SpeakAsync($"Hashing the specified  {isMasterish} master folder.");

      _TtlBytesDeleted = _TotalAdded = _TotalRenamed = _TotalDeleted = _TotalUpdated = _TotalHashed = 0;
      using (var db = new A0DbContext())
      {
        try
        {
          var sw = Stopwatch.StartNew();

          getAddAuditId(folder, db);

          using (var ha = SHA256Managed.Create()) //_md5 = MD5.Create(),		//  3.5/4.5 faster than SHA256/SHA512			//_sh5 = SHA512Managed.Create(),				//  128 char len					//   64 char len. 3/4 ~ SHA256/SHA512
          {
            var fis = await FileIO.getFileInfosAsync(folder);
            _ah.TotalFound = fis.Count();
            TtlLen = fis.Select(r => r.Length).Sum();
            PrgLen = 0L;
            var idx = 0L;

            //..synth.SpeakAsync($"{fis.Length} files found in the specified {isMasterish} master folder.");

#if ___
#if __
              Parallel.ForEach(fis.ToList(),
#else
            fis.ToList().ForEach(
#endif
              async fi => idx = await doFile(folder, isMasterish, db, sw, ha, idx, fi));
#else
            foreach (var fi in fis)
              idx = await doFile_OLD(folder, isMasterish, db, sw, ha, idx, fi);
#endif
          }

          SaveRpt += Environment.NewLine + db.GetDbChangesReport(5);
          SaveRpt += Environment.NewLine + db.TrySaveReport("r/s saved");
        }
        catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }
        finally { updateNewAuditHistRow(_ah.AuditTimeID, "Ini scan into DB"); }
      }

      synth.Speak("Hashing is done.");
    }
    async Task<long> doFile_OLD(string folder, bool isMasterish, A0DbContext db, Stopwatch sw, SHA256 ha, long idx, FileInfo fi)
    {
      {
        var pl = await smartHashFile_FastByFileInfo_OLD(folder, db, sw, ha, TtlLen, PrgLen, fi, isMasterish);
        lock (_thisLock)
        {
          PrgLen = pl;
          Interlocked.Increment(ref idx);
          if ((idx) % _dbPackLen == 0) { SaveRpt += Environment.NewLine + db.TrySaveReport($"saved; ttl {idx}"); }
        }
      }

      return idx;
    }

    public static string GetSmartFolder(string folder, int baseRootLen)
    {
      if (folder.ToLower().Contains("onedrive"))
        return folder.Substring(baseRootLen < folder.Length ? baseRootLen : 0);
      else
        return folder;
    }
    async Task UpdateDBfromFS(int v)
    {
      await Task.Delay(9);
      InfoMsg = "F7: ...";
      synth.SpeakAsync("F7: Updating DB with deletes from file system.");

      _TtlBytesDeleted = _TotalAdded = _TotalRenamed = _TotalDeleted = _TotalUpdated = _TotalHashed = 0;
      try
      {
        using (var db = new A0DbContext())
        {
          getAddAuditId("DB <== FS", db);

          //var oneDrBase = Path.GetFullPath(CurrentFolder).Substring(0, MasterSubBaseLen);

          //Trace.WriteLine(db.FileDetails.Where(r => string.IsNullOrEmpty(r.Note) || !r.Note.Contains(_nfo)).ToString(), "SQL");
          //var nonDelLst = db.FileDetails.Where(r => string.IsNullOrEmpty(r.Note) || !r.Note.Contains(_nfo)).ToList();
          //InfoMsg = $"F7: {db.FileDetails.ToList().Count():N0} total, {nonDelLst.Count():N0} non-deleted rows in db.";
          //await Task.Delay(9);
          //TtlLen = nonDelLst.Count();
          //PrgLen = 0;
          //foreach (var fd in nonDelLst)
          //{
          //  PrgLen++;

          //  var fdFullPathName = Path.Combine(oneDrBase, fd.FilePath.StartsWith(@"\") ? fd.FilePath.Substring(1) : fd.FilePath);
          //  if (!File.Exists(fdFullPathName))
          //  {
          //    smartUpdateNote(fd, $"{_nfo} {_ah.AuditTimeID:yyyy-MM-dd HH:mm}>\r\n");
          //    _TotalUpdated++;
          //  }
          //}

          SaveRpt += Environment.NewLine + db.GetDbChangesReport(5);

#if !DEBUG
          SaveRpt += Environment.NewLine + db.TrySaveReport("r/s saved");
#endif
        }
      }
      catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }
      finally
      {
        updateNewAuditHistRow(_ah.AuditTimeID, "F7: DB <== FS " + SaveRpt);
        synth.Speak("F7 Done.");
      }
    }

    async Task findDupesInDbAndDeleteThemFromFS_SUSPENDED(string mainFolder, int mainRootLen, string dupeFolder, int dupeRootLen, long minSz)
    {
      InfoMsg = "F2: ...";
      synth.SpeakAsync($"F2: Removing dupes from the specified dupe folder {dupeFolder}");
      synth.Speak("F2 suspended for now.");
      return;//  throw new Exception("SUSPENDED: ");

      _TtlBytesDeleted = _TotalAdded = _TotalRenamed = _TotalDeleted = _TotalUpdated = _TotalHashed = 0;
      using (var db = new A0DbContext())
      {
        try
        {
          var sw = Stopwatch.StartNew();

          getAddAuditId(dupeFolder.Substring(mainRootLen < dupeFolder.Length ? mainRootLen : 0), db);

          using (var ha = SHA256Managed.Create()) //_md5 = MD5.Create(),		//  3.5/4.5 faster than SHA256/SHA512			//_sh5 = SHA512Managed.Create(),				//  128 char len					//   64 char len. 3/4 ~ SHA256/SHA512
          {
            var delCandidates = FileIO.getFileInfosAsync(dupeFolder).Result.ToList().Where(r => r.Length > minSz).OrderByDescending(r => r.Length);
            _ah.TotalFound = delCandidates.Count();
            TtlLen = delCandidates.Select(r => r.Length).Sum();
            PrgLen = 0L;
            var idx = 0L;

            foreach (var delCandidate in delCandidates.OrderByDescending(r => r.Length))
            //Parallel.ForEach(delCandidates,
            //delCandidates.ToList().ForEach(
            //  async delCandidate =>
            {
              PrgLen = await makeSureItIsDupeNadSafeDelete(delCandidate, mainFolder, mainRootLen, dupeFolder, dupeRootLen, db, sw, ha, TtlLen, PrgLen); // 

              Interlocked.Increment(ref idx);
              lock (_thisLock) { if ((idx) % _dbPackLen == 0) { SaveRpt += Environment.NewLine + db.TrySaveReport($"saved; ttl {idx}"); } }
            }
            //);
          }

          SaveRpt += Environment.NewLine + db.GetDbChangesReport(5);
          SaveRpt += Environment.NewLine + db.TrySaveReport("r/s saved");
        }
        catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }
        finally
        {
          updateNewAuditHistRow(_ah.AuditTimeID, "UnDuping from " + dupeFolder);
        }
      }

      synth.Speak("F2 Done.");
    }
    async Task DeleteAllDupesAutoAndManual_F10()
    {
      await Task.Delay(9);
      InfoMsg = "F10: ...";
      synth.SpeakAsync("F10: Major Deleting all dupes from the main/base folder.");

      //throw new Exception("SUSPENDED: How to tell dupe from not? ");

      _TtlBytesDeleted = _TotalAdded = _TotalRenamed = _TotalDeleted = _TotalUpdated = _TotalHashed = 0;
      try
      {
        using (var db = new A0DbContext())
        {
          getAddAuditId("Delete all dupes", db);

          var dupesByHash = db.FileDetails.Where(r => string.IsNullOrEmpty(r.Note) || (!r.Note.Contains(_nfo) && !r.Note.Contains(_ndm))).GroupBy(g => g.FileHash).ToList().Select(r => new { r.Key, Cnt = r.Count(), Sze = r.Max(y => y.FileSize) }).Where(r => r.Cnt > 1).OrderByDescending(r => r.Sze).ToList();
          var dupesBySize = db.FileDetails.Where(r => string.IsNullOrEmpty(r.Note) || (!r.Note.Contains(_nfo) && !r.Note.Contains(_ndm))).GroupBy(g => g.FileSize).ToList().Select(r => new { r.Key, Cnt = r.Count(), Sze = r.Max(y => y.FileSize) }).Where(r => r.Cnt > 1).OrderByDescending(r => r.Sze).ToList();
          Trace.Write(/**/  db.FileDetails.Where(r => string.IsNullOrEmpty(r.Note) || (!r.Note.Contains(_nfo) && !r.Note.Contains(_ndm))).GroupBy(g => g.FileSize).   /**/  Select(r => new { r.Key, Cnt = r.Count(), Sze = r.Max(y => y.FileSize) }).Where(r => r.Cnt > 1).ToString(), "SQL");

          await Task.Delay(9);
          InfoMsg = $"F10: dupesBySize:{dupesBySize.Count:N0}, dupesByHash:{dupesByHash.Count:N0}.";
          Trace.WriteLine($"        F10: dupesBySize:{dupesBySize.Count:N0}, dupesByHash:{dupesByHash.Count:N0}.");

          TtlLen = (long)(_ah.TotalFound = dupesBySize.Count());
          PrgLen = 0; //here not file size but idx.
          var abort = false;
          dupesBySize.ForEach(dbs =>          //foreach (var r in dupesBySize)
          {
            PrgLen++;
            Trace.WriteLine($"{PrgLen,3} / {dupesBySize.Count})  {dbs.Cnt} matches by size of {dbs.Key:N0}.");
            if (!abort) lock (_thisLock) { if ((PrgLen) % 10 == 0) { SaveRpt += Environment.NewLine + db.TrySaveReport($"saved; ttl {PrgLen}"); } }

            db.FileDetails.Where(f => f.FileSize == dbs.Key).OrderBy(f => f.FileName.Length).ToList().ForEach(f => Trace.WriteLine($"  {f.FileSize / 1048576,8:N1}mb     {f.FilePath}\\{f.FileName}")); // db.FileDetails.Where(f => string.Compare(f.FileHash, r.Key, true) == 0).OrderBy(f => f.FileName.Length).ToList().ForEach(f => Trace.WriteLine("  {0,8:N1}k     {1}", f.FileSize / 1024, f.FilePath));

            var ary = db.FileDetails.Where(r => r.FileSize == dbs.Key && (string.IsNullOrEmpty(r.Note) || (!r.Note.Contains(_nfo) && !r.Note.Contains(_ndm)))).OrderBy(f => f.FileName.Length).ToArray();
            for (var i = 1; i < ary.Length; i++)
            {
              if (!abort)
                if (!tryRemoveDupe(ary[0], ary[i])) // SUSPENDED: How to tell dupe from not? 
                {
                  abort = true;
                  synth.SpeakAsync("Wait: aborting the F10 processing...");
                }
            }
          });

          SaveRpt += Environment.NewLine + db.GetDbChangesReport(5);

#if !DEBUG
          SaveRpt += Environment.NewLine + db.TrySaveReport("r/s saved");
#endif
        }
      }
      catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }
      finally
      {
        updateNewAuditHistRow(_ah.AuditTimeID, "F10: UnDuping-AutoAll " + SaveRpt);
        synth.Speak("F10 Done.");
      }
    }
    bool tryRemoveDupe(FileDetail fdMstr, FileDetail fdDupe)
    {
      var masterFullPathName = Path.Combine(fdMstr.FilePath, fdMstr.FileName);
      var doubleFullPathName = Path.Combine(fdDupe.FilePath, fdDupe.FileName);

      if (string.Compare(masterFullPathName, doubleFullPathName, true) == 0) { if (Debugger.IsAttached) Debugger.Break(); return false; }        // only if not the same file

      if (File.Exists(masterFullPathName) && File.Exists(doubleFullPathName)) // redundant but still...
      {
        if (string.Compare(fdMstr.FileHash, fdDupe.FileHash) == 0)
        {
          if (fdMstr.FileSize != fdDupe.FileSize)
            throw new Exception("File sizes differ!!!");

          safeDeleteFile(fdDupe, masterFullPathName, doubleFullPathName, "<Deleting full (hash+size) match of  {0}.>\r\n", fdDupe.FileSize);
          return true;
        }
        else
        {
          var nu = new DelPopup();
          _newUenWindowF.ShowNewWindow(nu, doubleFullPathName, masterFullPathName, fdDupe.FileHash, fdMstr.FileHash, fdDupe.FileSize, fdMstr.FileSize);
          if (nu.OkToDelete == null)
          {
            Trace.WriteLine("Close/Exit requested.......");
            return false;
          }
          else if (nu.OkToDelete.Value)
          {
            safeDeleteFile(fdDupe, masterFullPathName, doubleFullPathName, "<Deleting manually verified match of {0}>\r\n", fdDupe.FileSize);
            return true;
          }
          else
            smartUpdateNote(fdDupe, $"{_ndm} {masterFullPathName}>\r\n");
        }
      }
      else
      {
        smartUpdateNote(fdDupe, string.Format(_ndsne, masterFullPathName));
      }

      return true;
    }
    void safeDeleteFile(FileDetail dupe, string masterFullPathName, string delCandidate, string note, long fileSize, bool makeSureIsCameraRollOrAlike = true)
    {
      if (!File.Exists(masterFullPathName))
        throw new Exception("master does not exist.");

      //if (!(masterFullPathName.Contains(_MasterSignature) && (!masterFullPathName.Contains(_DoubleSignature) && !delCandidate.Contains(DoubleFolder))))				throw new Exception("Does not look like the master is in the right place: possibly deleting from the master instead of dupe location.");

      if (makeSureIsCameraRollOrAlike)
        if (delCandidate.Contains(_MasterSignature) && !delCandidate.Contains(_DoubleSignature))
          throw new Exception("DelCandidate does not belong to to-be-deleted-folder");

#if DEBUG
      Trace.WriteLine($"Deleting: {delCandidate}\r\n  Master: {masterFullPathName}");
      synth.Speak($"Deletion suspended for Debug mode.");
#else
      File.Delete(delCandidate);
#endif

      smartUpdateNote(dupe, string.Format(note, masterFullPathName));
      _TtlBytesDeleted += fileSize;
      _TotalDeleted++;
    }

    //Oct 2017
    async Task delSameNameSizeFromCameraRoll_F12(string folder, bool useSize)
    {
      const int d = -64;
      var i = 0;
      try
      {
        using (var db = new A0DbContext())
        {
          var fis = new DirectoryInfo(folder).GetFiles("*.*", SearchOption.AllDirectories);
          var msg = $"{fis.Length} files {fis.Sum(r => r.Length) / 1e9:N1} gb in {folder}";
          SaveRpt += Environment.NewLine + msg;
          Trace.WriteLine($"\r\n**>> {msg}:");
          foreach (var fi in fis)
          {
            var dupe = db.FileDetails.FirstOrDefault(r => r.FileName.Equals(fi.Name, StringComparison.OrdinalIgnoreCase) && (useSize ? r.FileSize == fi.Length : true) && r.FilePath.Equals(fi.DirectoryName, StringComparison.OrdinalIgnoreCase));
            var mstr = db.FileDetails.FirstOrDefault(r => r.FileName.Equals(fi.Name, StringComparison.OrdinalIgnoreCase) && (useSize ? r.FileSize == fi.Length : true) && !r.FilePath.Equals(fi.DirectoryName, StringComparison.OrdinalIgnoreCase));
            if (mstr != null && dupe != null)
            {
              if (!File.Exists(mstr.FullPath))
              {
                smartUpdateNote(dupe, $"<Dupe not deleted: Master is missing on FS: {mstr.FullPath}>\r\n");
              }
              else
              {
                Trace.WriteLine($"{++i,4}/{fis.Length,-4}   {fi.Length / 1e6,8:N2}\r\n-{fi.DirectoryName,d}{fi.Name,-40}{fi.Length / 1e6,8:N2}\r\n+{mstr.FilePath,d}{mstr.FileName,-40}{mstr.FileSize / 1e6,8:N2}");
#if DEBUG
                synth.Speak($"Deletion suspended for Debug mode.");
#else
                File.Delete(dupe.FullPath);
                smartUpdateNote(dupe, $"<Deleted as Dupe by Name {(useSize ? "and Size" : "only")}. Master: {mstr.FullPath}>\r\n");
#endif
              }
            }
          } // foreach

          SaveRpt += Environment.NewLine + db.GetDbChangesReport(5);
#if !DEBUG
          SaveRpt += Environment.NewLine + db.TrySaveReport(" F12 rows saved ");
#endif
        }
      }
      catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }

      await Task.Delay(9);
    }
  }
}
