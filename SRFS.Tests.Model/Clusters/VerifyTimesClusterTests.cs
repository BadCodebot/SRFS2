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
    public class VerifyTimesClusterTests {

        [TestMethod]
        public void VerifyTimesClusterSaveLoadTest() {
            ConfigurationTest.Initialize();

            using (var io = ConfigurationTest.CreateMemoryIO()) {
                SimpleClusterIO cio = new SimpleClusterIO(io);

                Random r = new Random();
                VerifyTimesCluster csc = new VerifyTimesCluster(2);
                csc.Initialize();
                for (int i = 0; i < csc.Count; i++) csc[i] = new DateTime(r.Next());
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
                Assert.AreEqual(clusterType, ClusterType.VerifyTimeTable);
                offset += sizeof(ClusterType);

                int nextClusterAddress = b.ToInt32(offset);
                Assert.AreEqual(nextClusterAddress, Constants.NoAddress);
                offset += sizeof(int);

                long[] cs = new long[csc.Count];
                for (int i = 0; i < csc.Count; i++) cs[i] = b.ToInt64(offset + i * sizeof(long));
                Assert.IsTrue(cs.SequenceEqual(from d in csc select d.Ticks));

                VerifyTimesCluster csc2 = new VerifyTimesCluster(2);
                cio.Load(csc2);
                Assert.AreEqual(csc.ID, csc2.ID);
                Assert.AreEqual(csc.Type, csc2.Type);
                Assert.AreEqual(csc.NextClusterAddress, csc2.NextClusterAddress);

                Assert.IsTrue(csc.SequenceEqual(csc2));
            }
        }
    }
}
