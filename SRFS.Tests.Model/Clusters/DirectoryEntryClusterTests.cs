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

namespace SRFS.Tests.Model.Clusters {

    [TestClass]
    public class DirectoryEntryClusterTests {

        [TestMethod]
        public void DirectoryEntryClusterSaveLoadTest() {
            ConfigurationTest.Initialize();

            using (var io = new MemoryIO(30 * 1024 * 1024, 512)) {

                DirectoryEntryCluster csc = new DirectoryEntryCluster();

                DateTime creationTime = new DateTime(2000, 1, 1);
                DateTime lastWriteTime = new DateTime(2001, 1, 1);
                DateTime lastAccessTime = new DateTime(2002, 1, 1);
                SecurityIdentifier group = new SecurityIdentifier(WellKnownSidType.NullSid, null);
                SecurityIdentifier owner = WindowsIdentity.GetCurrent().Owner;

                for (int i = 0; i < csc.Count; i++) csc[i] = new DirectoryEntry(i, i.ToString()) {
                    Attributes = System.IO.FileAttributes.Normal,
                    CreationTime = creationTime,
                    LastWriteTime = lastWriteTime,
                    LastAccessTime = lastAccessTime,
                    ParentID = i + 1,
                    Group = new SecurityIdentifier(WellKnownSidType.NullSid, null),
                    Owner = owner
                };
                csc.Address = 2;
                csc.NextClusterAddress = 3;
                csc.Save(io);

                int offset = 0;
                ByteBlock b = new ByteBlock(io.Bytes, 
                    2 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);

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
                Assert.AreEqual(clusterType, ClusterType.DirectoryTable);
                offset += sizeof(ClusterType);

                int nextClusterAddress = b.ToInt32(offset);
                Assert.AreEqual(nextClusterAddress, 3);
                offset += sizeof(int);

                DirectoryEntry[] cs = new DirectoryEntry[csc.Count];
                for (int i = 0; i < csc.Count; i++) {
                    if (b.ToBoolean(offset)) {
                        cs[i] = b.ToDirectoryEntry(offset + i * DirectoryEntryCluster.ElementLength + sizeof(bool));
                    } else {
                        cs[i] = null;
                    }
                }
                Assert.IsTrue(cs.SequenceEqual(csc));

                DirectoryEntryCluster csc2 = new DirectoryEntryCluster();
                csc2.Address = 2;
                csc2.Load(io);
                Assert.AreEqual(csc.ID, csc2.ID);
                Assert.AreEqual(csc.Type, csc2.Type);
                Assert.AreEqual(csc.NextClusterAddress, csc2.NextClusterAddress);

                Assert.IsTrue(csc.SequenceEqual(csc2));
            }
        }
    }
}
