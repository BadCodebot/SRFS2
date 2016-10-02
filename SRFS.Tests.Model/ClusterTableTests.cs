using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model;
using SRFS.Model.Data;
using SRFS.Model.Clusters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SRFS.IO;

namespace SRFS.Tests.Model {

    [TestClass]
    public class ClusterTableTests {

        private int verifyProperties(ByteBlock b, ClusterType expectedType, int expectedNextClusterAddress) {

            int offset = 0;

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
            Assert.AreEqual(clusterType, expectedType);
            offset += sizeof(ClusterType);

            int nextClusterAddress = b.ToInt32(offset);
            Assert.AreEqual(nextClusterAddress, expectedNextClusterAddress);
            offset += sizeof(int);

            return offset;
        }

        [TestMethod]
        public void IntClusterTableTest() {
            ConfigurationTest.Initialize();

            using (var io = new MemoryIO(30 * 1024 * 1024, 512)) {

                Random r = new Random();

                ClusterTable<int> ct = new ClusterTable<int>(new IntArrayCluster[] {
                    new IntArrayCluster() { Address = 2, Type = ClusterType.BytesUsedTable },
                    new IntArrayCluster() { Address = 4, Type = ClusterType.BytesUsedTable }
                });

                ClusterTable<int> ct2 = new ClusterTable<int>(new IntArrayCluster[] {
                    new IntArrayCluster() { Address = 2, Type = ClusterType.BytesUsedTable },
                    new IntArrayCluster() { Address = 4, Type = ClusterType.BytesUsedTable }
                });

                ct.Flush(io);
                ct2.Load(io);

                // Check that the cluster is written and everything is zeroed

                ByteBlock b = new ByteBlock(io.Bytes, 2 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);
                int offset = verifyProperties( b, ClusterType.BytesUsedTable, 4);
                for (int i = 0; i < ArrayCluster.CalculateElementCount(sizeof(int)); i++) {
                    Assert.AreEqual(b.ToInt32(offset + i * sizeof(int)), 0);
                }

                b = new ByteBlock(io.Bytes, 4 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);
                offset = verifyProperties( b, ClusterType.BytesUsedTable, Constants.NoAddress);
                for (int i = 0; i < ArrayCluster.CalculateElementCount(sizeof(int)); i++) {
                    Assert.AreEqual(b.ToInt32(offset + i * sizeof(int)), 0);
                }

                for (int i = 0; i < ct2.Count; i++) {
                    Assert.AreEqual(ct2[i], 0);
                }

                // Now randomize the contents
                for (int i = 0; i < ct.Count; i++) ct[i] = r.Next();
                ct.Flush(io);

                b = new ByteBlock(io.Bytes, 2 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);
                offset = verifyProperties(b, ClusterType.BytesUsedTable, 4);
                int index = 0;
                for (int i = 0; i < ArrayCluster.CalculateElementCount(sizeof(int)); i++, index++) {
                    Assert.AreEqual(b.ToInt32(offset + i * sizeof(int)), ct[index]);
                }

                b = new ByteBlock(io.Bytes, 4 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);
                offset = verifyProperties(b, ClusterType.BytesUsedTable, Constants.NoAddress);
                for (int i = 0; i < ArrayCluster.CalculateElementCount(sizeof(int)); i++, index++) {
                    Assert.AreEqual(b.ToInt32(offset + i * sizeof(int)), ct[index]);
                }

                ct2.Load(io);
                for (int i = 0; i < ct2.Count; i++) {
                    Assert.AreEqual(ct2[i], ct[i]);
                }

                // Add a cluster
                ct.AddCluster(new IntArrayCluster() { Address = 7, Type = ClusterType.BytesUsedTable });
                ct2.AddCluster(new IntArrayCluster() { Address = 7, Type = ClusterType.BytesUsedTable });
                ct.Flush(io);

                // Make sure that next cluster is updated
                b = new ByteBlock(io.Bytes, 4 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);
                verifyProperties(b, ClusterType.BytesUsedTable, 7);

                // check the new cluster and assure everything is zero
                b = new ByteBlock(io.Bytes, 7 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);
                offset = verifyProperties(b, ClusterType.BytesUsedTable, Constants.NoAddress);
                for (int i = 0; i < ArrayCluster.CalculateElementCount(sizeof(int)); i++) {
                    Assert.AreEqual(b.ToInt32(offset + i * sizeof(int)), 0);
                }

                ct2.Load(io);
                for (int i = 0; i < ct2.Count; i++) {
                    Assert.AreEqual(ct2[i], ct[i]);
                }

                Assert.AreEqual(ct2.Count, 3 * ArrayCluster.CalculateElementCount(sizeof(int)));

                // Remove a cluster
                ct.RemoveLastCluster();
                ct.Flush(io);

                // Make sure that the last cluster is updated
                b = new ByteBlock(io.Bytes, 4 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);
                verifyProperties(b, ClusterType.BytesUsedTable, Constants.NoAddress);
            }
        }

    }
}
