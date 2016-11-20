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

namespace SRFS.Tests.Model.Clusters {

    [TestClass]
    public class ClusterStatesClusterTests {

        [TestMethod]
        public void ClusterStateArraySaveLoadTest() {
            ConfigurationTest.Initialize();

            using (var io = ConfigurationTest.CreateMemoryIO()) {
                SimpleClusterIO cio = new SimpleClusterIO(io);

                Random r = new Random();
                ClusterStatesCluster csc = new ClusterStatesCluster(2);
                for (int i = 0; i < csc.Count; i++) csc[i] = (ClusterState)r.Next(16);
                csc.Initialize();
                csc.NextClusterAddress = Constants.NoAddress;
                cio.Save(csc);

                int offset = 0;
                DataBlock b = new DataBlock(io.Bytes, 2 * Configuration.Geometry.BytesPerCluster, Configuration.Geometry.BytesPerCluster);

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
                Assert.AreEqual(clusterType, ClusterType.ClusterStateTable);
                offset += sizeof(ClusterType);

                int nextClusterAddress = b.ToInt32(offset);
                Assert.AreEqual(nextClusterAddress, Constants.NoAddress);
                offset += sizeof(int);

                ClusterState[] cs = new ClusterState[csc.Count];
                for (int i = 0; i < csc.Count; i++) cs[i] = (ClusterState)b.ToByte(offset + i * sizeof(ClusterState));
                Assert.IsTrue(cs.SequenceEqual(csc));

                ClusterStatesCluster csc2 = new ClusterStatesCluster(2);
                cio.Load(csc2);
                Assert.AreEqual(csc.VolumeID, csc2.VolumeID);
                Assert.AreEqual(csc.Type, csc2.Type);
                Assert.AreEqual(csc.NextClusterAddress, csc2.NextClusterAddress);

                Assert.IsTrue(csc.SequenceEqual(csc2));
            }
        }
    }
}
