using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SRFS.IO;
using SRFS.Model;
using SRFS.Model.Data;
using System.Security.Principal;
using System.Security.Permissions;
using System.Security.AccessControl;

namespace SRFS.Tests.Model {

    [TestClass]
    public class FileSystemTests {

        [TestMethod]
        public void FileSystemCreateTest() {
            ConfigurationTest.Initialize();
            Random r = new Random(1234);

            using (var io = ConfigurationTest.CreateMemoryIO()) {

                FileSystem fs = FileSystem.Create(io);
                fs.Flush();

                ClusterState[] clusterStateArray = new ClusterState[Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount];
                for (int i = 0; i < clusterStateArray.Length; i++) {
                    if ((fs.GetClusterState(i) & ClusterState.System) == 0) {
                        ClusterState c = (ClusterState)r.Next(16);
                        clusterStateArray[i] = c;
                        fs.SetClusterState(i, c);
                    } else {
                        clusterStateArray[i] = fs.GetClusterState(i);
                    }
                }

                int[] nextClusterArray = new int[Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount];
                for (int i = 0; i < nextClusterArray.Length; i++) {
                    if ((fs.GetClusterState(i) & ClusterState.System) == 0) {
                        int c = r.Next();
                        nextClusterArray[i] = c;
                        fs.SetNextClusterAddress(i, c);
                    } else {
                        nextClusterArray[i] = fs.GetNextClusterAddress(i);
                    }
                }

                int[] bytesUsedArray = new int[Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount];
                for (int i = 0; i < bytesUsedArray.Length; i++) {
                    if ((fs.GetClusterState(i) & ClusterState.System) == 0) {
                        int c = r.Next();
                        bytesUsedArray[i] = c;
                        fs.SetBytesUsed(i, c);
                    } else {
                        bytesUsedArray[i] = fs.GetBytesUsed(i);
                    }
                }

                DateTime[] verifyTimeArray = new DateTime[Configuration.Geometry.ClustersPerTrack * Configuration.Geometry.TrackCount];
                for (int i = 0; i < verifyTimeArray.Length; i++) {
                    if (i >= Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount ||
                        (fs.GetClusterState(i) & ClusterState.System) == 0) {
                        DateTime c = new DateTime(r.Next());
                        verifyTimeArray[i] = c;
                        fs.SetVerifyTime(i, c);
                    } else {
                        verifyTimeArray[i] = fs.GetVerifyTime(i);
                    }
                }

                fs.Dispose();

                string volumeName = Configuration.VolumeName;
                Geometry geometry = Configuration.Geometry;
                CryptoSettings cryptoSettings = Configuration.CryptoSettings;
                Options options = Configuration.Options;
                Guid guid = Configuration.FileSystemID;
                Configuration.Reset();

                Configuration.CryptoSettings = cryptoSettings;
                Configuration.Options = options;

                fs = FileSystem.Mount(io);

                Assert.AreEqual(volumeName, Configuration.VolumeName);
                Assert.AreEqual(geometry, Configuration.Geometry);
                Assert.AreEqual(guid, Configuration.FileSystemID);

                for (int i = 0; i < clusterStateArray.Length; i++) {
                    Assert.AreEqual(clusterStateArray[i], fs.GetClusterState(i));
                }

                for (int i = 0; i < nextClusterArray.Length; i++) {
                    Assert.AreEqual(nextClusterArray[i], fs.GetNextClusterAddress(i));
                }

                for (int i = 0; i < bytesUsedArray.Length; i++) {
                    Assert.AreEqual(bytesUsedArray[i], fs.GetBytesUsed(i));
                }

                for (int i = 0; i < verifyTimeArray.Length; i++) {
                    Assert.AreEqual(verifyTimeArray[i],fs.GetVerifyTime(i));
                }

                fs.Dispose();
            }
        }

        private static int nextID = 0;

        [TestMethod]
        public void FileSystemPopulate() {
            ConfigurationTest.Initialize();
            Random r = new Random(1234);

            using (var io = ConfigurationTest.CreateMemoryIO()) {

                FileSystem fs = FileSystem.Create(io);
                fs.Flush();

                SortedList<int, DirInfo> dirs = new SortedList<int, DirInfo>();
                SortedList<int, FileInfo> files = new SortedList<int, FileInfo>();

                Directory d = fs.RootDirectory;
                dirs.Add(fs.RootDirectory.ID, new DirInfo(d));

                for (int i = 0; i < 1000; i++) {
                    var parent = dirs.Values[r.Next(dirs.Count)];
                    d = fs.CreateDirectory(parent.Directory, $"dir{nextID}");
                    nextID++;

                    DirInfo info = new DirInfo(d);
                    dirs.Add(d.ID, info);
                    dirs[d.ParentID].Subdirs.Add(d.ID);

                    FileSystemAccessRule accessRule = new FileSystemAccessRule(
                        WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, InheritanceFlags.None,
                        PropagationFlags.None, AccessControlType.Allow);
                    fs.AddAccessRule(d, accessRule);
                    info.AccessRules.Add(accessRule);

                    FileSystemAuditRule auditRule = new FileSystemAuditRule(
                        WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, InheritanceFlags.None,
                        PropagationFlags.None, AuditFlags.Success);
                    fs.AddAuditRule(d, auditRule);
                    info.AuditRules.Add(auditRule);
                }

                for (int i = 0; i < 1000; i++) {
                    var parent = dirs.Values[r.Next(dirs.Count)];
                    File f = fs.CreateFile(parent.Directory, $"file{nextID}");
                    nextID++;

                    FileInfo info = new FileInfo(f);
                    files.Add(f.ID, info);
                    dirs[f.ParentID].Files.Add(f.ID);

                    FileSystemAccessRule accessRule = new FileSystemAccessRule(
                        WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, InheritanceFlags.None,
                        PropagationFlags.None, AccessControlType.Allow);
                    fs.AddAccessRule(f, accessRule);
                    info.AccessRules.Add(accessRule);

                    FileSystemAuditRule auditRule = new FileSystemAuditRule(
                        WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, InheritanceFlags.None,
                        PropagationFlags.None, AuditFlags.Success);
                    fs.AddAuditRule(f, auditRule);
                    info.AuditRules.Add(auditRule);
                }

                checkDirectories(fs, fs.RootDirectory, dirs, files);
                fs.Dispose();

                CryptoSettings cryptoSettings = Configuration.CryptoSettings;
                Options options = Configuration.Options;
                Configuration.Reset();
                Configuration.CryptoSettings = cryptoSettings;
                Configuration.Options = options;

                fs = FileSystem.Mount(io);
                checkDirectories(fs, fs.RootDirectory, dirs, files);
            }
        }

        private static bool AccessRulesEqual(FileSystemAccessRule r1, FileSystemAccessRule r2) {
            return
                r1.IdentityReference == r2.IdentityReference &&
                r1.FileSystemRights == r2.FileSystemRights &&
                r1.InheritanceFlags == r2.InheritanceFlags &&
                r1.PropagationFlags == r2.PropagationFlags &&
                r1.AccessControlType == r2.AccessControlType;
        }

        private static bool AuditRulesEqual(FileSystemAuditRule r1, FileSystemAuditRule r2) {
            return
                r1.IdentityReference == r2.IdentityReference &&
                r1.FileSystemRights == r2.FileSystemRights &&
                r1.InheritanceFlags == r2.InheritanceFlags &&
                r1.PropagationFlags == r2.PropagationFlags &&
                r1.AuditFlags == r2.AuditFlags;
        }

        private void checkDirectories(FileSystem fs, Directory current, SortedList<int, DirInfo> dirs, SortedList<int,FileInfo> files) {
            Assert.AreEqual(current, dirs[current.ID].Directory);

            foreach (var r in dirs[current.ID].AccessRules) {
                Assert.IsTrue(fs.GetAccessRules(current).Any(x => AccessRulesEqual(r,x)));
            }
            foreach (var r in dirs[current.ID].AuditRules) {
                Assert.IsTrue(fs.GetAuditRules(current).Any(x => AuditRulesEqual(r,x)));
            }

            var fsSubdirs = fs.GetContainedDirectories(current);
            var fsFiles = fs.GetContainedFiles(current);
            foreach (var x in fsSubdirs) {
                Assert.IsTrue(dirs.Values[current.ID].Subdirs.Contains(x.Value.ID));
                checkDirectories(fs, x.Value, dirs, files);
            }
            foreach (int x in dirs[current.ID].Subdirs) {
                Directory xdir = dirs[x].Directory;
                string xname = xdir.Name;
                Assert.IsTrue(fsSubdirs.ContainsKey(xname));
            }
            foreach (var f in fsFiles) {
                Assert.IsTrue(dirs.Values[current.ID].Files.Contains(f.Value.ID));

                foreach (var r in fs.GetAccessRules(f.Value)) {
                    Assert.IsTrue(files[f.Value.ID].AccessRules.Any(x => AccessRulesEqual(r, x)));
                }
                foreach (var r in fs.GetAuditRules(f.Value)) {
                    Assert.IsTrue(files[f.Value.ID].AuditRules.Any(x => AuditRulesEqual(r, x)));
                }

                Assert.AreEqual(f.Value, files[f.Value.ID].File);
            }
            foreach (int f in dirs[current.ID].Files) {
                File xfile = files[f].File;
                string xname = xfile.Name;
                Assert.IsTrue(fsFiles.ContainsKey(xname));
            }
        }

        private class DirInfo {
            public DirInfo(Directory dir) { this.Directory = dir; }
            public readonly Directory Directory;
            public readonly HashSet<int> Subdirs = new HashSet<int>();
            public readonly HashSet<int> Files = new HashSet<int>();
            public readonly List<FileSystemAccessRule> AccessRules = new List<FileSystemAccessRule>();
            public readonly List<FileSystemAuditRule> AuditRules = new List<FileSystemAuditRule>();
        }

        private class FileInfo {
            public FileInfo(File file) { this.File = file; }
            public readonly File File;
            public readonly List<FileSystemAccessRule> AccessRules = new List<FileSystemAccessRule>();
            public readonly List<FileSystemAuditRule> AuditRules = new List<FileSystemAuditRule>();
        }
    }
}
