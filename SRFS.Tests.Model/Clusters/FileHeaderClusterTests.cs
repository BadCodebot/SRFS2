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
    public class FileHeaderClusterTests {

        private const int Address = 2;
        private const int Seed = 149987;

        [TestMethod]
        public void FileHeaderSaveLoadTest() {
            ConfigurationTest.Initialize();

            using (var io = ConfigurationTest.CreateMemoryIO()) {
                SimpleClusterIO cio = new SimpleClusterIO(io);

                Random r = new Random(Seed);
                FileHeaderCluster csc = new FileHeaderCluster(Address);
                csc.Initialize();

                int fileID = r.Next();
                int nextClusterAddress = r.Next();
                int bytesUsed = r.Next();
                DateTime writeTime = new DateTime(2005, 1, 1);
                int parentID = r.Next();
                string name = r.Next().ToString();

                // File System Cluster fields
                csc.FileID = fileID;
                csc.NextClusterAddress = nextClusterAddress;
                csc.BytesUsed = bytesUsed;
                csc.WriteTime = writeTime;

                // File Header fields
                csc.ParentID = parentID;
                csc.Name = name;

                byte[] data = new byte[FileHeaderCluster.DataSize];
                for (int i = 0; i < data.Length; i++) data[i] = (byte)(r.Next() & byte.MaxValue);

                csc.Data.Set(0, data);
                cio.Save(csc);

                int offset = 0;
                DataBlock b = new DataBlock(
                    io.Bytes,
                    Address * Configuration.Geometry.BytesPerCluster,
                    Configuration.Geometry.BytesPerCluster);

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
                Assert.AreEqual(clusterType, ClusterType.FileHeader);
                offset += sizeof(ClusterType);

                Assert.AreEqual(fileID, b.ToInt32(offset));
                offset += sizeof(int);

                Assert.AreEqual(nextClusterAddress, b.ToInt32(offset));
                offset += sizeof(int);

                Assert.AreEqual(bytesUsed, b.ToInt32(offset));
                offset += sizeof(int);

                Assert.AreEqual(writeTime, new DateTime(b.ToInt64(offset)));
                offset += sizeof(long);

                Assert.AreEqual(parentID, b.ToInt32(offset));
                offset += sizeof(int);

                int nameLength = b.ToByte(offset);
                offset += sizeof(byte);
                Assert.AreEqual(name, b.ToString(offset, nameLength));
                offset += Constants.MaximumNameLength * sizeof(char);

                byte[] encryptionThumbprintBytes = b.ToByteArray(offset, KeyThumbprint.Length);
                Assert.IsTrue(encryptionThumbprintBytes.SequenceEqual(Configuration.CryptoSettings.EncryptionKeyThumbprint.Bytes));
                offset += KeyThumbprint.Length;

                byte[] publicKeyBytes = b.ToByteArray(offset, PublicKey.Length);
                offset += PublicKey.Length;

                int dataLength = Configuration.Geometry.BytesPerCluster - offset;
                int padding = dataLength % 16;
                dataLength -= padding;
                Assert.AreEqual(data.Length, dataLength);
                offset += padding;

                using (ECDiffieHellmanCng dest = new ECDiffieHellmanCng(Configuration.CryptoSettings.DecryptionKey))
                using (AesCng aes = new AesCng()) {

                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.None;
                    aes.Key = dest.DeriveKeyMaterial(new PublicKey(publicKeyBytes).GetCngKey());
                    aes.IV = new byte[16];

                    using (var decryptor = aes.CreateDecryptor()) {
                        byte[] decryptedData = b.TransformFinalBlock(decryptor, offset, dataLength);
                        for (int i = 0; i < dataLength; i++) {
                            Assert.AreEqual(data[i], decryptedData[i]);
                        }
                    }
                }

                FileHeaderCluster csc2 = new FileHeaderCluster(Address);
                cio.Load(csc2);

                Assert.IsTrue(csc2.Marker.SequenceEqual(Constants.SrfsMarker));
                Assert.IsTrue(csc2.Version.SequenceEqual(Constants.CurrentVersion));
                Assert.AreEqual(csc2.VolumeID, Configuration.FileSystemID);
                Assert.AreEqual(csc2.Type, ClusterType.FileHeader);
                Assert.AreEqual(csc2.FileID, fileID);
                Assert.AreEqual(csc2.NextClusterAddress, nextClusterAddress);
                Assert.AreEqual(csc2.BytesUsed, bytesUsed);
                Assert.AreEqual(csc2.WriteTime, writeTime);
                Assert.AreEqual(csc2.ParentID, parentID);
                Assert.AreEqual(csc2.Name, name);

                for (int i = 0; i < dataLength; i++) {
                    Assert.AreEqual(data[i], csc2.Data.ToByte(i));
                }
            }
        }
    }
}
