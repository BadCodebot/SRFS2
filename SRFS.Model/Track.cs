using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.ReedSolomon;
using SRFS.Model.Clusters;
using System.Threading;
using System.ComponentModel;
namespace SRFS.Model {
    public class Track {

        public Track(FileSystem fileSystem, int trackNumber) {
            _fileSystem = fileSystem;
            _trackNumber = trackNumber;
        }

        public IEnumerable<int> DataClusters {
            get {
                int tracksPerRegion = Configuration.Geometry.TrackCount / 8;
                int region = _trackNumber / tracksPerRegion;

                int clustersPerSection = Configuration.Geometry.ParityClustersPerTrack;
                int sectionIndex = _trackNumber % tracksPerRegion;

                int offset = region * tracksPerRegion * Configuration.Geometry.DataClustersPerTrack + sectionIndex * clustersPerSection;

                int n = 0;
                while (true) {
                    for (int i = 0; i < Configuration.Geometry.ParityClustersPerTrack; i++, n++) {
                        if (n == Configuration.Geometry.DataClustersPerTrack) yield break;
                        yield return offset + i;
                    }
                    offset += clustersPerSection * tracksPerRegion;
                }
            }
        }

        public IEnumerable<int> ParityClusters {
            get {
                int start = Configuration.Geometry.DataClustersPerTrack * Configuration.Geometry.TrackCount +
                    _trackNumber * Configuration.Geometry.ParityClustersPerTrack;
                for (int i = start; i < start + Configuration.Geometry.ParityClustersPerTrack; i++) {
                    yield return i;
                }
            }
        }

        private bool IsClusterDirty(ClusterState s) => (!s.IsSystem()) && (s.IsModified() | s.IsUnwritten());
        public bool DataModified {
            get {
                return DataClusters.Any(x => IsClusterDirty(_fileSystem.GetClusterState(x)));
            }
        }

        public bool Used {
            get {
                return DataClusters.Any(x => (_fileSystem.GetClusterState(x) & ClusterState.Used) != 0);
            }
        }

        public bool ParityWritten {
            get {
                return ParityClusters.All(x => (_fileSystem.GetClusterState(x) & ClusterState.Unwritten) == 0);
            }
        }

        public bool UpToDate {
            get {
                return (!Used) || ((!DataModified) && ParityWritten);
            }
        }

        public class UpdateParityStatus : INotifyPropertyChanged {

            public int Cluster {
                get {
                    lock (_lock) return _cluster;
                }
                set {
                    lock (_lock) _cluster = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Cluster)));
                }
            }
            private int _cluster = -1;

            private object _lock = new object();

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public Task UpdateParity(bool force, UpdateParityStatus status, CancellationToken token) {
            return Task.Run(() => updateParityAsyncInternal(force,status,token), token);
        }

        private void updateParityAsyncInternal(bool force, UpdateParityStatus status, CancellationToken token) {
            if (!force && !DataModified && ParityWritten) return;

            int dataClustersPerTrack = Configuration.Geometry.DataClustersPerTrack;
            int parityClustersPerTrack = Configuration.Geometry.ParityClustersPerTrack;
            int bytesPerCluster = Configuration.Geometry.BytesPerCluster;

            using (var p = new Parity(dataClustersPerTrack, parityClustersPerTrack, bytesPerCluster / 2)) {
                int codewordExponent = dataClustersPerTrack + parityClustersPerTrack - 1;
                byte[] emptyCluster = null;
                int clustersComplete = -1;
                foreach (var absoluteClusterNumber in DataClusters) {
                    ClusterState state = _fileSystem.GetClusterState(absoluteClusterNumber);
                    if (!state.IsSystem()) {
                        if ((state & ClusterState.Unwritten) != 0) {
                            if (emptyCluster == null) {
                                EmptyCluster c = new EmptyCluster(absoluteClusterNumber);
                                _fileSystem.ClusterIO.Save(c);
                                emptyCluster = new byte[bytesPerCluster];
                                c.Save(emptyCluster, 0);
                            }
                            p.Calculate(emptyCluster, 0, codewordExponent);
                            state &= ~ClusterState.Unwritten;
                            _fileSystem.SetClusterState(absoluteClusterNumber, state);
                        } else {
                            Cluster c = new Cluster(absoluteClusterNumber, bytesPerCluster);
                            _fileSystem.ClusterIO.Load(c);
                            byte[] bytes = new byte[bytesPerCluster];
                            c.Save(bytes, 0);
                            p.Calculate(bytes, 0, codewordExponent);
                        }
                    }

                    clustersComplete++;
                    status.Cluster = clustersComplete;
                    codewordExponent--;
                    if (token.IsCancellationRequested) return;
                }

                for (int i = 0; i < parityClustersPerTrack; i++) {
                    ParityCluster c = new ParityCluster(_fileSystem.BlockSize, _trackNumber, i);
                    byte[] bytes = new byte[bytesPerCluster];
                    p.GetParity(bytes, 0, parityClustersPerTrack - 1 - i);
                    c.Data.Set(0, bytes);
                    _fileSystem.ClusterIO.Save(c);
                    _fileSystem.SetClusterState(c.ClusterAddress, ClusterState.Parity);

                    clustersComplete++;
                    status.Cluster = clustersComplete;
                    if (token.IsCancellationRequested) return;
                }
            }

            // Update states after the parity is modified in case there is an error
            foreach (var i in DataClusters) {
                ClusterState state = _fileSystem.GetClusterState(i) & (~ClusterState.Modified);
                _fileSystem.SetClusterState(i, state);
            }
            _fileSystem.Flush();
        }

        public void UpdateParity(bool force = false) {
            if (!force && !DataModified && ParityWritten) return;

            int dataClustersPerTrack = Configuration.Geometry.DataClustersPerTrack;
            int parityClustersPerTrack = Configuration.Geometry.ParityClustersPerTrack;
            int bytesPerCluster = Configuration.Geometry.BytesPerCluster;

            using (var p = new Parity(dataClustersPerTrack, parityClustersPerTrack, bytesPerCluster / 2)) {
                int codewordExponent = dataClustersPerTrack + parityClustersPerTrack - 1;
                byte[] emptyCluster = null;
                foreach (var absoluteClusterNumber in DataClusters) {
                    ClusterState state = _fileSystem.GetClusterState(absoluteClusterNumber);
                    if (!state.IsSystem()) {
                        if ((state & ClusterState.Unwritten) != 0) {
                            if (emptyCluster == null) {
                                EmptyCluster c = new EmptyCluster(absoluteClusterNumber);
                                Console.WriteLine($"Saving empty cluster {absoluteClusterNumber}");
                                _fileSystem.ClusterIO.Save(c);
                                emptyCluster = new byte[bytesPerCluster];
                                c.Save(emptyCluster, 0);
                            }
                            p.Calculate(emptyCluster, 0, codewordExponent);
                            state &= ~ClusterState.Unwritten;
                            _fileSystem.SetClusterState(absoluteClusterNumber, state);
                        } else {
                            Cluster c = new Cluster(absoluteClusterNumber, bytesPerCluster);
                            Console.WriteLine($"Loading cluster {absoluteClusterNumber}");
                            _fileSystem.ClusterIO.Load(c);
                            byte[] bytes = new byte[bytesPerCluster];
                            c.Save(bytes, 0);
                            p.Calculate(bytes, 0, codewordExponent);
                        }
                    }

                    codewordExponent--;
                }

                for (int i = 0; i < parityClustersPerTrack; i++) {
                    ParityCluster c = new ParityCluster(_fileSystem.BlockSize, _trackNumber, i);
                    byte[] bytes = new byte[bytesPerCluster];
                    p.GetParity(bytes, 0, parityClustersPerTrack - 1 - i);
                    c.Data.Set(0, bytes);
                    Console.WriteLine($"Saving Parity Cluster {c.ClusterAddress}");
                    _fileSystem.ClusterIO.Save(c);
                    _fileSystem.SetClusterState(c.ClusterAddress, ClusterState.Parity);
                }
            }

            // Update states after the parity is modified in case there is an error
            foreach (var i in DataClusters) {
                ClusterState state = _fileSystem.GetClusterState(i) & (~ClusterState.Modified);
                _fileSystem.SetClusterState(i, state);
            }
        }

        public bool Repair() {
            // We need to enforce that hashes and signatures are checked (what happens if signatures aren't?  What is not protected?  What happens?
            if (DataModified || !ParityWritten) return false;

            int dataClustersPerTrack = Configuration.Geometry.DataClustersPerTrack;
            int parityClustersPerTrack = Configuration.Geometry.ParityClustersPerTrack;
            int bytesPerCluster = Configuration.Geometry.BytesPerCluster;

            List<int> errorExponents = new List<int>();

            using (var p = new Syndrome(dataClustersPerTrack, parityClustersPerTrack, bytesPerCluster / 2)) {
                int codewordExponent = dataClustersPerTrack + parityClustersPerTrack - 1;
                foreach (var absoluteClusterNumber in DataClusters) {
                    if (!_fileSystem.GetClusterState(absoluteClusterNumber).IsSystem()) {
                        Cluster c = new Cluster(absoluteClusterNumber, bytesPerCluster);
                        Console.WriteLine($"Loading cluster {absoluteClusterNumber}");
                        try {
                            _fileSystem.ClusterIO.Load(c);
                            byte[] bytes = new byte[bytesPerCluster];
                            c.Save(bytes, 0);
                            p.AddCodewordSlice(bytes, 0, codewordExponent);
                        } catch (System.IO.IOException) {
                            // We need to catch something specific to a hash failure or something.  Or force no auto check, then check manually.
                            Console.WriteLine($"Error in data cluster {absoluteClusterNumber}");
                            errorExponents.Add(codewordExponent);
                        }
                    }
                    codewordExponent--;
                }

                int parityNumber = 0;
                foreach (var absoluteClusterNumber in ParityClusters) {
                    ParityCluster c = new ParityCluster(_fileSystem.BlockSize, _trackNumber, parityNumber);
                    Console.WriteLine($"Loading parity cluster {absoluteClusterNumber}");
                    try {
                        _fileSystem.ClusterIO.Load(c);
                        byte[] bytes = c.Data.ToByteArray(0, bytesPerCluster);
                        p.AddCodewordSlice(bytes, 0, codewordExponent);
                    } catch (System.IO.IOException) {
                        Console.WriteLine($"Error in parity cluster {absoluteClusterNumber}");
                        errorExponents.Add(codewordExponent);
                    }
                    codewordExponent--;
                    parityNumber++;
                }

                if (errorExponents.Count == 0) {
                    byte[] values = new byte[bytesPerCluster];
                    for (int i = 0; i < parityClustersPerTrack; i++) {
                        p.GetSyndromeSlice(values, 0, i);
                        for (int j = 0; j < bytesPerCluster; j++) {
                            if (values[j] != 0) return false;
                        }
                    }
                    return true;
                } else {
                    // Throw an exception or specify the error somehow
                    if (errorExponents.Count > parityClustersPerTrack) return false;

                    using (var r = new Repair(p, dataClustersPerTrack + parityClustersPerTrack, errorExponents)) {
                        byte[] bytes = new byte[bytesPerCluster];
                        int[] dataClusters = DataClusters.ToArray();
                        int index = 0;
                        foreach (var e in errorExponents) {
                            r.Correction(index++, bytes, 0);
                            if (e < parityClustersPerTrack) {
                                // it is a parity cluster
                                ParityCluster c = new ParityCluster(_fileSystem.BlockSize, _trackNumber, parityClustersPerTrack - 1 - e);
                                c.Data.Set(0, bytes);
                                Console.WriteLine($"Repairing parity {c.ClusterAddress} at {c.AbsoluteAddress}");
                                _fileSystem.ClusterIO.Save(c);
                            } else {
                                // Need to do this without creating a cluster, since that will change the signature
                                int i = dataClustersPerTrack + parityClustersPerTrack - 1 - e;
                                Cluster c = new Cluster(dataClusters[i], bytesPerCluster);
                                c.Load(bytes, 0);
                                Console.WriteLine($"Repairing data {c.ClusterAddress} at {c.AbsoluteAddress}");
                                _fileSystem.ClusterIO.Save(c);
                            }
                        }
                    }

                    return true;
                }
            }
        }

        public bool VerifyParity() {
            if (DataModified || !ParityWritten) return false;

            int dataClustersPerTrack = Configuration.Geometry.DataClustersPerTrack;
            int parityClustersPerTrack = Configuration.Geometry.ParityClustersPerTrack;
            int bytesPerCluster = Configuration.Geometry.BytesPerCluster;

            using (var p = new Syndrome(dataClustersPerTrack, parityClustersPerTrack, bytesPerCluster / 2)) {
                int codewordExponent = dataClustersPerTrack + parityClustersPerTrack - 1;
                foreach (var absoluteClusterNumber in DataClusters) {
                    if (!_fileSystem.GetClusterState(absoluteClusterNumber).IsSystem()) {
                        Cluster c = new Cluster(absoluteClusterNumber, bytesPerCluster);
                        Console.WriteLine($"Loading cluster {absoluteClusterNumber}");
                        _fileSystem.ClusterIO.Load(c);
                        byte[] bytes = new byte[bytesPerCluster];
                        c.Save(bytes, 0);
                        p.AddCodewordSlice(bytes, 0, codewordExponent);
                    }
                    codewordExponent--;
                }

                int parityNumber = 0;
                foreach (var absoluteClusterNumber in ParityClusters) {
                    ParityCluster c = new ParityCluster(_fileSystem.BlockSize, _trackNumber, parityNumber);
                    Console.WriteLine($"Loading parity cluster {absoluteClusterNumber}");
                    _fileSystem.ClusterIO.Load(c);
                    byte[] bytes = c.Data.ToByteArray(0, bytesPerCluster);
                    p.AddCodewordSlice(bytes, 0, codewordExponent);
                    codewordExponent--;
                    parityNumber++;
                }

                byte[] values = new byte[bytesPerCluster];
                for (int i = 0; i < parityClustersPerTrack; i++) {
                    p.GetSyndromeSlice(values, 0, i);
                    for (int j = 0; j < bytesPerCluster; j++) {
                        if (values[j] != 0) return false;
                    }
                }
            }

            return true;
        }

        public int Number => _trackNumber;

        private FileSystem _fileSystem;
        private int _trackNumber;
    }
}
