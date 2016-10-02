using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SRFS.Model;
using System.Security.Cryptography;
using SRFS.Model.Clusters;
using SRFS.IO;
using SRFS.Model.Data;
using System.Security.Principal;
using System.Security.AccessControl;

namespace SRFS.Tests.Model.Clusters {

    [TestClass]
    public class ObjectArrayClusterTests {

        private int verifyProperties(ByteBlock b, int offset, ClusterType type) {

            byte[] marker = b.ToByteArray(offset, Constants.SrfsMarker.Length);
            Assert.IsTrue(marker.SequenceEqual(Constants.SrfsMarker));
            offset += marker.Length;

            byte[] version = b.ToByteArray(offset, Constants.CurrentVersion.Length);
            Assert.IsTrue(version.SequenceEqual(Constants.CurrentVersion));
            offset += version.Length;

            Guid guid = new Guid(b.ToByteArray(offset, Constants.GuidLength));
            Assert.AreEqual(guid, Configuration.FileSystemID);
            offset += Constants.GuidLength;

            byte[] signatureBytes = b.ToByteArray(offset, Signature.Length);
            offset += Signature.Length;

            byte[] thumbprintBytes = b.ToByteArray(offset, KeyThumbprint.Length);
            Assert.IsTrue(thumbprintBytes.SequenceEqual(Configuration.CryptoSettings.SigningKeyThumbprint.Bytes));
            offset += KeyThumbprint.Length;

            byte[] hashBytes = b.ToByteArray(offset, 32);
            offset += 32;

            ClusterType clusterType = (ClusterType)b.ToByte(offset);
            Assert.AreEqual(clusterType, type);
            offset += sizeof(ClusterType);

            int nextClusterAddress = b.ToInt32(offset);
            Assert.AreEqual(nextClusterAddress, 3);
            offset += sizeof(int);

            return offset;
        }

        private static DateTime creationTime = new DateTime(2000, 1, 1);
        private static DateTime lastWriteTime = new DateTime(2001, 1, 1);
        private static DateTime lastAccessTime = new DateTime(2002, 1, 1);
        private static SecurityIdentifier group = new SecurityIdentifier(WellKnownSidType.NullSid, null);
        private static SecurityIdentifier owner = WindowsIdentity.GetCurrent().Owner;
        private static int Address = 2;
        private static int NextClusterAddress = 3;
        private static int BlockSize = 512;
        private static int RandomNumberGeneratorSeed = 1298;
        private static Random _random = new Random(RandomNumberGeneratorSeed);

        private Directory createDirectory(int index) {
            return new Directory(index, index.ToString()) {
                Attributes = System.IO.FileAttributes.Normal,
                CreationTime = creationTime,
                LastWriteTime = lastWriteTime,
                LastAccessTime = lastAccessTime,
                ParentID = index + 1,
                Group = group,
                Owner = owner
            };
        }

        private File createFileEntry(int index) {
            return new File(index, index.ToString()) {
                Attributes = System.IO.FileAttributes.Normal,
                CreationTime = creationTime,
                LastWriteTime = lastWriteTime,
                LastAccessTime = lastAccessTime,
                ParentID = index + 1,
                Group = group,
                Owner = owner,
                Length = _random.Next(),
                FirstCluster = _random.Next()
            };
        }

        private SrfsAccessRule createAccessRule(int index) {
            FileSystemAccessRule r = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, AccessControlType.Allow);
            return new SrfsAccessRule(FileSystemObjectType.File, _random.Next(), r);
        }

        private SrfsAuditRule createAuditRule(int index) {
            FileSystemAuditRule r = new FileSystemAuditRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, AuditFlags.Success);
            return new SrfsAuditRule(FileSystemObjectType.File, _random.Next(), r);
        }

        private void saveLoadTest<T>(Func<ObjectArrayCluster<T>> clusterFactory, Func<int, T> elementFactory,
            Func<ByteBlock, int, T> elementReader, int elementLength) where T : class {

            ConfigurationTest.Initialize();

            using (var io = new MemoryIO(30 * Configuration.Geometry.BytesPerCluster, BlockSize)) {

                var csc = clusterFactory();

                for (int i = 0; i < csc.Count; i++) csc[i] = elementFactory(i);

                csc.NextClusterAddress = NextClusterAddress;
                csc.Save(io);

                int offset = 0;
                ByteBlock b = new ByteBlock(io.Bytes,
                    Address * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);

                offset = verifyProperties(b, offset, csc.Type);

                T[] cs = new T[csc.Count];
                for (int i = 0; i < csc.Count; i++) {
                    if (b.ToBoolean(offset)) {
                        offset += sizeof(bool);
                        cs[i] = elementReader(b, offset);
                    } else {
                        offset += sizeof(bool);
                        cs[i] = null;
                    }
                    offset += elementLength;
                }
                Assert.IsTrue(cs.SequenceEqual(csc));

                var csc2 = clusterFactory();
                csc2.Address = Address;
                csc2.Load(io);
                Assert.AreEqual(csc.ID, csc2.ID);
                Assert.AreEqual(csc.Type, csc2.Type);
                Assert.AreEqual(csc.NextClusterAddress, csc2.NextClusterAddress);

                Assert.IsTrue(csc.SequenceEqual(csc2));
            }
        }

        [TestMethod]
        public void DirectoryEntryClusterSaveLoadTest() {

            saveLoadTest(() => Directory.CreateArrayCluster(Address), 
                createDirectory, 
                (b, offset) => new Directory(b, offset), 
                Directory.StorageLength);
        }

        [TestMethod]
        public void FileEntryClusterSaveLoadTest() {

            saveLoadTest(() => File.CreateArrayCluster(Address),
                createFileEntry,
                (b, offset) => new File(b, offset),
                File.StorageLength);
        }

        [TestMethod]
        public void AccessRuleClusterSaveLoadTest() {

            saveLoadTest(() => SrfsAccessRule.CreateArrayCluster(Address),
                createAccessRule,
                (b, offset) => new SrfsAccessRule(b, offset),
                SrfsAccessRule.StorageLength);
        }

        [TestMethod]
        public void AuditRuleClusterSaveLoadTest() {

            saveLoadTest(() => SrfsAuditRule.CreateArrayCluster(Address),
                createAuditRule,
                (b, offset) => new SrfsAuditRule(b, offset),
                SrfsAuditRule.StorageLength);
        }
    }
}
