using AAV.Db.EF;
using AAV.Sys.Ext;
using Db.OneDriveAudit.DbModel;
using Microsoft.SqlServer.Types;
using MVVM.Common;
using System;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ODA.VM.VM
{
  public partial class MainVM : BindableBaseViewModel
  {
    AuditHist _ah;
    int _TotalAdded = 0, _TotalRenamed = 0, _TotalDeleted = 0, _TotalUpdated = 0, _TotalHashed = 0;
    long _TtlBytesDeleted = 0;
    static readonly DateTime _AuditTimeID = SecondRoundedNow();


    void getAddAuditId(string pathRoot, A0DbContext db)
    {
      try
      {
        #region https://www.andrewcbancroft.com/2017/03/27/solving-spatial-types-and-functions-are-not-available-with-entity-framework/
        SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);

        //SqlProviderServices.SqlServerTypesAssemblyName = "Microsoft.SqlServer.Types, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
        //To avoid hard - coding the assembly name you can use 
        SqlProviderServices.SqlServerTypesAssemblyName = typeof(SqlGeography).Assembly.FullName; // – Samuel Jack Oct 17 '17 at 10:24
        #endregion

        Debug.WriteLine(db.AuditHists.Count());
        _ah = db.AuditHists.FirstOrDefault(r => r.AuditTimeID == _AuditTimeID);
        if (_ah != null)
          return;

        _ah = db.AuditHists.Add(new AuditHist
        {
          AuditTimeID = _AuditTimeID,
          PathRoot = pathRoot,
          TotalFound = 0,
          TotalAdded = 0,
          TotalHashed = 0,
        });
      }
      catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }
    }

    void updateNewAuditHistRow(DateTime auditTimeID, string mode)
    {
      using (var db = new A0DbContext())
      {
        var ah = db.AuditHists.FirstOrDefault(r => r.AuditTimeID == auditTimeID) ?? db.AuditHists.Local.FirstOrDefault(r => r.AuditTimeID == auditTimeID);
        if (ah == null)
          return;

        ah.TotalAdded = _TotalAdded;
        ah.TotalHashed = _TotalHashed;
        ah.FoundRenamed = _TotalRenamed;

        InfoMsg += $"\r\n FINAL:  Found {_ah.TotalFound:N0},  Added {_TotalAdded:N0},  Hashed {_TotalHashed:N0},  Moved {_TotalRenamed:N0},  Updated {_TotalUpdated:N0},  Deleted {_TotalDeleted:N0} ({_TtlBytesDeleted / (1024 * 1024):N0} mb)";

        ah.Note = $"{mode}: {InfoMsg.Substring(8)} \r\n";

        SaveRpt += Environment.NewLine + db.TrySaveReport("r/s saved");
      }
    }

    async Task<long> addOrUpdateToDb_SansHash(string folder, A0DbContext db, Stopwatch sw, SHA256 ha, long ttlLen, long prgLen, FileInfo exisitingFile, bool isMasterish)
    {
      prgLen = await updateMsgInfo(sw, ttlLen, prgLen, exisitingFile, "F1");

      try
      {
        FileDetail exg = null;
        lock (_thisLock)
        {
          exg = db.FileDetails.FirstOrDefault(r =>
              r.FileSize == exisitingFile.Length &&
              string.Compare(r.FileName, exisitingFile.Name, true) == 0 &&
              string.Compare(r.FilePath, exisitingFile.DirectoryName, true) == 0
            );
        }
        if (exg != null) // if already in DB and has hash - move on.
        {
          lock (_thisLock)
          {
            if (exg.LastSeen != _ah.AuditTimeID.Date)
            {
              exg.LastSeen = _ah.AuditTimeID.Date;
              exg.ModifiedAt = _ah.AuditTimeID;
            }
          }
        }
        else // not in DB yet by filename + Len - add it:
        {
          lock (_thisLock)
          {
            addNewFD_SansHash(db, exisitingFile, isMasterish ? "<new master file>" : "<new from dupe location>");
          }
        }
      }
      catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }

      return prgLen;
    }
    async Task<long> smartHashFile_FastByFileInfo_OLD(string folder, A0DbContext db, Stopwatch sw, SHA256 ha, long ttlLen, long prgLen, FileInfo fi, bool isMasterish)
    {
      prgLen = await updateMsgInfo(sw, ttlLen, prgLen, fi, "F1");

      try
      {
        FileDetail exg = null;
        lock (_thisLock)
        {
          exg = db.FileDetails.FirstOrDefault(r =>
              r.FileSize == fi.Length &&
              string.Compare(r.FileName, fi.Name, true) == 0 &&
              string.Compare(r.FilePath, fi.DirectoryName, true) == 0
            );
        }
        if (exg != null) // if already in DB and has hash - move on.
        {
          lock (_thisLock)
          {
            if (exg.LastSeen != _ah.AuditTimeID.Date)
              exg.LastSeen = _ah.AuditTimeID.Date;
          }

          if (string.IsNullOrEmpty(exg.FileHash)) // rare, but
          {
            var fh = await GetFileHashAsync(fi, ha);
            lock (_thisLock)
            {
              _TotalHashed++;
              exg.FileHash = fh;
            }
          }
        }
        else // not in DB yet by filename - hash + find in DB by hash + find in FS + add-if a dupe | update paths-if moved(not exists in DB specified location):
        {
          var hash = await GetFileHashAsync(fi, ha);
          lock (_thisLock)
          {
            _TotalHashed++;

            var exgByHash = db.FileDetails.Local.FirstOrDefault(r => string.Compare(r.FileHash, hash) == 0) ?? db.FileDetails.FirstOrDefault(r => string.Compare(r.FileHash, hash) == 0);
            if (exgByHash == null)
            {
              addNewFD(db, fi, hash, isMasterish ? "<new master file>" : "<new from dupe location>");
            }
            else
            {
              var sameInDb = Path.Combine(exgByHash.FilePath, exgByHash.FileName);
              if (File.Exists(sameInDb)) // a match by hash exists in FS: add as copy 
              {
                addNewFD(db, fi, hash, $"<Copy is at  {exgByHash.FilePath}>\r\n");
              }
              else              // must've been moved/renamed: update DB with the latest filename.
              {
                //if(fi.DirectoryName.ToLower().Contains(exgByHash.FilePath.ToLower()))

                smartUpdateNote(exgByHash, $"<Moved from  {exgByHash.FilePath}>\r\n");
                exgByHash.LastSeen = _ah.AuditTimeID;
                exgByHash.FileName = fi.Name;
                exgByHash.FileExtn = fi.Extension;
                exgByHash.FilePath = fi.DirectoryName;
              }
            }
          }
        }
      }
      catch (Exception ex) { ex.Log(); ErrorMsg = ex.Message; throw; }

      return prgLen;
    }
    async Task<long> makeSureItIsDupeNadSafeDelete(FileInfo delCandidate, string mainFolder, int mainRootLen, string dupeFolder, int dupeRootLen, A0DbContext db, Stopwatch sw, SHA256 ha, long ttlLen, long prgLen)
    {
      prgLen = await updateMsgInfo(sw, ttlLen, prgLen, delCandidate, "F2");

      var superPath = Path.GetFullPath(delCandidate.FullName).Substring(mainRootLen); // only works within OneDrive.
      var oneDrBase = Path.GetFullPath(mainFolder).Substring(0, mainRootLen);


      FileDetail dupeInDb = null;
      lock (_thisLock)
      {
        dupeInDb = db.FileDetails.FirstOrDefault(r =>
            r.FileSize == delCandidate.Length &&
            string.Compare(r.FileName, delCandidate.Name, true) == 0 && (
            string.Compare(r.FilePath, superPath, true) == 0 ||             // for within  OneDrive
            r.FilePath.ToLower().Contains(delCandidate.FullName.ToLower())  // for without OneDrive
            )
          );
        if (dupeInDb != null)
          if (dupeInDb.LastSeen != _ah.AuditTimeID.Date)
            dupeInDb.LastSeen = _ah.AuditTimeID.Date;
      }

      var hash = await GetFileHashAsync(delCandidate, ha);
      lock (_thisLock)
      {
        _TotalHashed++;

        if (!db.FileDetails.Any(r => string.Compare(r.FileHash, hash) == 0))
        {
          addNewFD(db, delCandidate, hash, $"<Exisiting file does not exist: {delCandidate.FullName}>\r\n");
        }
        else
        {
          var dupeCount = db.FileDetails.Count(r => r.FileSize == delCandidate.Length);
          foreach (var exgByLen in db.FileDetails.Where(r => r.FileSize == delCandidate.Length).ToList()) // db.FileDetails.Where(r => string.Compare(r.FileHash, hash) == 0).ToList().ForEach(exgByHash =>
          {
            var dbFullPathName = Path.Combine(oneDrBase, exgByLen.FilePath.StartsWith(@"\") ? exgByLen.FilePath.Substring(1) : exgByLen.FilePath);

            Trace.WriteLine($"{dupeCount} matches, \n {delCandidate.FullName} - fic\n {dbFullPathName} - db ver\n");

            if (string.Compare(delCandidate.FullName, dbFullPathName, true) != 0) // only if not the same record in db
            {
              if (File.Exists(dbFullPathName))  // if found in db file exists at the db specified location - safe to delete the dupe
              {
                if (File.Exists(delCandidate.FullName))   // we could have deleted the dupe already.
                {
                  if (string.Compare(exgByLen.FileHash, hash) == 0)
                  {
                    safeDeleteFile(dupeInDb, dbFullPathName, delCandidate.FullName, "<Deleted-Auto as Dupe by HASH. Org: {0}", delCandidate.Length);
                    break;
                  }
                  else
                  {
                    var nu = new DelPopup();
                    _newUenWindowF.ShowNewWindow(nu, delCandidate.FullName, dbFullPathName, hash, exgByLen.FileHash, delCandidate.Length, exgByLen.FileSize);
                    if (nu.OkToDelete == null)
                    {
                      OnRequestClose();
                      break;
                    }
                    else if (nu.OkToDelete.Value)
                    {
                      safeDeleteFile(dupeInDb, dbFullPathName, delCandidate.FullName, "<Deleted as Dupe by FileLen and Look. Org: {0}", delCandidate.Length);
                      break;
                    }
                  }
                }
              }
              else // if the entry in DB does not exist in FS, update the DB with the current DUPE's values (for full local OneDrive mirror)
              {
                smartUpdateNote(exgByLen, $"<Moved from  {exgByLen.FilePath}>\r\n");
                exgByLen.LastSeen = _ah.AuditTimeID;
                exgByLen.FileName = delCandidate.Name;
                exgByLen.FileExtn = delCandidate.Extension;
                exgByLen.FilePath = superPath;
                _TotalRenamed++;
              }
            }
          }
        }
      }

      return prgLen;
    }

    async Task<long> updateMsgInfo(Stopwatch sw, long ttlLen, long prgLen, FileInfo fi, string ff)
    {
      prgLen += fi.Length;
      var bpm = prgLen / sw.Elapsed.TotalMinutes;
      var rmg = bpm == 0 ? 0 : (ttlLen - prgLen) / bpm;

      await Task.Delay(1);//InfoMsg does not work without!!!!!!!!!!!
                          ////Application.Current.Dispatcher.BeginInvoke(new Action(() => {
      InfoMsg = string.Format("{10}: {0,7:N1} %   {1,5:N1} mb/m   {2,5:N1} m ► {3:ddd HH:mm:ss}   Files:  found {4:N0},  added {5:N0},  hashed {6:N0},  updated {7:N0},  deleted {8:N0} ({9:N0} mb)",
        100.0 * prgLen / ttlLen, .000001 * bpm, rmg, DateTime.Now.AddMinutes(rmg), _ah.TotalFound, _TotalAdded, _TotalHashed, _TotalUpdated, _TotalDeleted, _TtlBytesDeleted / (1024 * 1024), ff);

      return prgLen;
    }

    void addNewFD(A0DbContext db, FileInfo fi, string hash, string note)
    {
      //nogo: SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory); //jul 2017 - Solving ‘Spatial types and functions are not available’ with Entity Framework
      db.FileDetails.Add(new FileDetail
      {
        AddedByAuditTimeID = _ah.AuditTimeID,
        LastSeen = _ah.AuditTimeID,
        FilePath = fi.DirectoryName,
        FileName = fi.Name,
        FileExtn = fi.Extension,
        FileCreated = fi.CreationTime,
        FileSize = fi.Length,
        FileHash = hash,
        Note = note,
        CreatedAt = _ah.AuditTimeID,
      });

      _TotalAdded++;
    }
    void addNewFD_SansHash(A0DbContext db, FileInfo fi, string note)
    {
      //nogo: SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory); //jul 2017 - Solving ‘Spatial types and functions are not available’ with Entity Framework
      db.FileDetails.Add(new FileDetail
      {
        AddedByAuditTimeID = _ah.AuditTimeID,
        LastSeen = _ah.AuditTimeID,
        FilePath = fi.DirectoryName,
        FileName = fi.Name,
        FileExtn = fi.Extension,
        FileCreated = fi.CreationTime,
        FileSize = fi.Length,
        //leHash = hash,
        Note = note,
        CreatedAt = _ah.AuditTimeID,
      });

      _TotalAdded++;
    }

    static void smartUpdateNote(FileDetail fd, string note)
    {
      Trace.WriteLine(note);

      if (fd != null && (string.IsNullOrEmpty(fd.Note) || !fd.Note.Contains(note)))
        lock (_thisLock)
        {
          fd.Note += note;
          if (fd.Note.Length > 8000)
            fd.Note = fd.Note.Substring(0, 8000);
        }
    }

    async Task<long> smartHashFile_SlowByHash(A0DbContext db, Stopwatch sw, SHA256 ha, long ttl, long prg, FileInfo fi)
    {
      {
        var hash = await GetFileHashAsync(fi, ha); // var  hash1 = GetMd5Hash(ha, "      string source = Hello World!   ");
        _TotalHashed++;
        prg += fi.Length;
        var bps = prg / sw.Elapsed.TotalSeconds;
        var rmg = (ttl - prg) / bps;
        InfoMsg = $"F?: {100.0 * prg / ttl,7:N1} %   {.000001 * bps,5:N1} mb/s   {rmg,5:N0} s ==> {DateTime.Now.AddSeconds(rmg):MM-dd HH:mm:ss}   ";

        var exg = db.FileDetails.FirstOrDefault(r => string.Compare(r.FileHash, hash) == 0);
        if (exg != null)
        {
          exg.LastSeen = _ah.AuditTimeID;
          if (string.Compare(exg.FileName, fi.Name, true) != 0 || string.Compare(exg.FilePath, Path.GetFullPath(fi.FullName), true) != 0)
            exg.Note += "Dupe!! ";
        }
        else
        {
          addNewFD(db, fi, hash, $"<Never tried method: {fi.FullName}>\r\n");
        }
      }

      return prg;
    }

    public static string GetFileHash(FileInfo fi, HashAlgorithm ha)
    {
      using (var stream = fi.OpenRead())
      {
        return BitConverter.ToString(ha.ComputeHash(stream)).Replace("-", "");         //var lng = BitConverter.ToInt64(hash, 0); Trace.Write(string.Format("{0,20} - {1,66} ", lng, str));
      }
    }
    public async Task<string> GetFileHashAsync(FileInfo fi, HashAlgorithm ha)
    {
      FHashed = fi.FullName;

      using (var stream = fi.OpenRead())
      {
        var t = Task.Run(() => BitConverter.ToString(ha.ComputeHash(stream)).Replace("-", ""));
        t.Wait();
        return await t;
      }
    }


    public static DateTime MinuteRoundedNow()
    {
      var n = DateTime.Now;
      var m = new DateTime(n.Year, n.Month, n.Day, n.Hour, n.Minute, 0);
      return m;
    }
    public static DateTime SecondRoundedNow()
    {
      var n = DateTime.Now;
      var m = new DateTime(n.Year, n.Month, n.Day, n.Hour, n.Minute, n.Second);
      return m;
    }
  }
}
/*
SELECT        ID, AddedByAuditTimeID, FileName, FileExtn, FilePath, FileSize, FileCreated, FileHash, LastSeen, Note, Metadata, Location, LocLat, LocLon
FROM            a00.FileDetail
WHERE        (FileSize IN
                             (SELECT        FileSize
                               FROM            a00.FileDetail AS FileDetail_1
                               GROUP BY FileSize
                               HAVING         (COUNT(*) > 1)))
ORDER BY FileSize desc, FilePath

SELECT        ID, AddedByAuditTimeID, FileName, FileExtn, FilePath, FileSize, FileCreated, FileHash, LastSeen, Note, Metadata, Location, LocLat, LocLon
FROM            a00.FileDetail
WHERE        (FileHash IN
                             (SELECT        FileHash
                               FROM            a00.FileDetail AS FileDetail_1
                               GROUP BY FileHash
                               HAVING         (COUNT(*) > 1)))
ORDER BY FileSize DESC, FilePath



SELECT        COUNT(*) AS Expr1, AddedByAuditTimeID
FROM            a00.FileDetail
GROUP BY AddedByAuditTimeID
ORDER BY AddedByAuditTimeID DESC


-- SQL at home network: https://www.youtube.com/watch?v=5UkHYNwUtCo#t=400.850701


SELECT        SUBSTRING(Note, 1, 7) AS path, COUNT(*) AS Expr1, SUM(FileSize) / 1000000 AS Sz
FROM            a00.FileDetail
GROUP BY SUBSTRING(Note, 1, 7)

SELECT        SUBSTRING(FilePath, 1, 15) AS path, COUNT(*) AS Expr1, SUM(FileSize) / 1000000 AS Sz
FROM            a00.FileDetail
GROUP BY SUBSTRING(FilePath, 1, 15)

SELECT        SUBSTRING(Note, 1, 15) AS path, COUNT(*) AS Expr1, SUM(FileSize) / 1000000 AS Sz
FROM            a00.FileDetail
GROUP BY SUBSTRING(Note, 1, 15)





    Stupid error:
        Spatial types and functions are not available for this provider because the assembly 'Microsoft.SqlServer.Types' version 10 or higher could not be found. 
    solution:
        https://stackoverflow.com/questions/43221467/assembly-microsoft-sqlserver-types-version-10-or-higher-could-not-be-found
        just run the msi from - http://go.microsoft.com/fwlink/?LinkID=239644&clcid=0x409

*/
