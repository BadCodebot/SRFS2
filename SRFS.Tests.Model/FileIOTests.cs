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
using SRFS.Model.Clusters;

namespace SRFS.Tests.Model {

    [TestClass]
    public class FileIOTests {

        [TestMethod]
        public void FileIOTest() {
            ConfigurationTest.Initialize();
            Random r = new Random(1234);

            using (var io = ConfigurationTest.CreateMemoryIO()) {

                FileSystem fs = FileSystem.Create(io);

                byte[] data = new byte[1024 * 1024];
                r.NextBytes(data);

                Directory d = fs.RootDirectory;
                File f = fs.CreateFile(d, "TEST");
                using (FileIO fio = new FileIO(fs, f)) {
                    Assert.AreEqual(data.Length, fio.WriteFile(data, 0));
                    for (int j = 0; j < 100; j++) {
                        switch (r.Next(3)) {
                            case 0: {
                                    // Internal Overwrite
                                    int offset = r.Next(data.Length);
                                    int count = r.Next(data.Length - offset);
                                    if (count != 0) {
                                        byte[] newData = new byte[count];
                                        r.NextBytes(newData);
                                        Assert.AreEqual(newData.Length, fio.WriteFile(newData, offset));
                                        Buffer.BlockCopy(newData, 0, data, offset, count);
                                        Assert.AreEqual(data.Length, f.Length);
                                    }
                                    break;
                                }
                            case 1: {
                                    // Append
                                    int count = r.Next(2 * 1024 * 1024 - (int)f.Length);
                                    if (count != 0) {
                                        int offset = (int)f.Length;
                                        byte[] newData = new byte[count];
                                        r.NextBytes(newData);
                                        Assert.AreEqual(newData.Length, fio.WriteFile(newData, offset));
                                        byte[] dt = new byte[count + data.Length];
                                        Buffer.BlockCopy(data, 0, dt, 0, offset);
                                        Buffer.BlockCopy(newData, 0, dt, offset, count);
                                        data = dt;
                                        Assert.AreEqual(data.Length, f.Length);
                                    }
                                    break;
                                }
                            case 2: {
                                    // Set Length
                                    int count = r.Next(2 * 1024 * 1024);
                                    fio.SetEndOfFile(count);
                                    byte[] newData = new byte[count];
                                    Buffer.BlockCopy(data, 0, newData, 0, Math.Min(count, data.Length));
                                    data = newData;
                                    Assert.AreEqual(data.Length, f.Length);
                                    break;
                                }
                        }
                    }
                }

                int length = (int)f.Length;
                int clusters = length <= FileHeaderCluster.DataSize ? 1 :
                    (length - FileHeaderCluster.DataSize + FileDataCluster.DataSize - 1) / FileDataCluster.DataSize + 1;
                Assert.AreEqual(clusters, (from s in fs.ClusterStates where (s & ClusterState.Used) != 0 && (s & ClusterState.System) == 0 select s).Count());

                fs.Dispose();

                CryptoSettings cryptoSettings = Configuration.CryptoSettings;
                Options options = Configuration.Options;
                Configuration.Reset();
                Configuration.CryptoSettings = cryptoSettings;
                Configuration.Options = options;

                fs = FileSystem.Mount(io);
                byte[] data2 = new byte[data.Length];

                File f2 = fs.GetContainedFiles(fs.RootDirectory)["TEST"];
                Assert.AreEqual(f.Length, f2.Length);
                using (FileIO fio = new FileIO(fs, f2)) {
                    Assert.AreEqual(data2.Length, fio.ReadFile(data2, 0));
                    Assert.AreEqual(0, fio.ReadFile(data2, data2.Length));
                }

                Assert.IsTrue(data.SequenceEqual(data2));

                length = (int)f2.Length;
                clusters = length <= FileHeaderCluster.DataSize ? 1 :
                    (length - FileHeaderCluster.DataSize + FileDataCluster.DataSize - 1) / FileDataCluster.DataSize + 1;
                Assert.AreEqual(clusters, (from s in fs.ClusterStates where (s & ClusterState.Used) != 0 && (s & ClusterState.System) == 0 select s).Count());

                using (FileIO fio = new FileIO(fs, f2)) {
                    fio.SetEndOfFile(0);
                }

                Assert.AreEqual(0, f2.Length);
                Assert.AreEqual(0, (from s in fs.ClusterStates where (s & ClusterState.Used) != 0 && (s & ClusterState.System) == 0 select s).Count());

                fs.Dispose();

                Configuration.Reset();
                Configuration.CryptoSettings = cryptoSettings;
                Configuration.Options = options;

                fs = FileSystem.Mount(io);
                File f3 = fs.GetContainedFiles(fs.RootDirectory)["TEST"];
                Assert.AreEqual(0, f3.Length);
                Assert.AreEqual(0, (from s in fs.ClusterStates where (s & ClusterState.Used) != 0 && (s & ClusterState.System) == 0 select s).Count());
            }
        }

        [TestMethod]
        public void FillFileSystemTest() {
            ConfigurationTest.Initialize();
            Random r = new Random(1234);

            using (var io = ConfigurationTest.CreateMemoryIO()) {

                FileSystem fs = FileSystem.Create(io);

                byte[] data = new byte[1024*1024];

                Directory d = fs.RootDirectory;
                File f = fs.CreateFile(d, "TEST");
                int totalBytesWritten = 0;
                using (FileIO fio = new FileIO(fs, f)) {
                    int bytesWritten = 0;
                    do {
                        r.NextBytes(data);
                        bytesWritten = fio.WriteFile(data, f.Length);
                        totalBytesWritten += bytesWritten;
                    } while (bytesWritten != 0);
                }

                fs.Dispose();

                CryptoSettings cryptoSettings = Configuration.CryptoSettings;
                Options options = Configuration.Options;
                Configuration.Reset();
                Configuration.CryptoSettings = cryptoSettings;
                Configuration.Options = options;

                fs = FileSystem.Mount(io);

                r = new Random(1234);
                f = fs.GetContainedFiles(fs.RootDirectory)["TEST"];
                Assert.AreEqual(f.Length, totalBytesWritten);
                int totalBytesRead = 0;
                byte[] compare = new byte[1024 * 1024];
                using (FileIO fio = new FileIO(fs, f)) {
                    int bytesRead = 0;
                    do {
                        r.NextBytes(data);
                        bytesRead = fio.ReadFile(compare, totalBytesRead);
                        totalBytesRead += bytesRead;
                        for (int i = 0; i < bytesRead; i++) {
                            Assert.AreEqual(compare[i], data[i]);
                        }
                    } while (bytesRead != 0);
                }

                Assert.AreEqual(totalBytesRead, totalBytesWritten);

                fs.Dispose();
            }
        }
    }
}
