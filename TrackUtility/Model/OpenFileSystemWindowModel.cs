using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.IO;
using System.ComponentModel;
using System.Security.Cryptography;
using System.IO;
using System.Security;
using SRFS.Model;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace TrackUtility.Model {
    public class OpenFileSystemWindowModel : IDataErrorInfo, INotifyPropertyChanged {

        public IEnumerable<Drive> AvailableDrives => Drive.Drives.OrderBy(x => x.Name, StringComparer.InvariantCultureIgnoreCase);

        public Drive Drive {
            get { return _drive; }
            set { _drive = value; notifyPropertyChanged(); Partition = null; }
        }
        private Drive _drive;

        public Partition Partition {
            get { return _partition; }
            set { _partition = value; notifyPropertyChanged(); }
        }
        private Partition _partition;

        public string KeyFolderPath {
            get { return _keyFolderPath; }
            set { _keyFolderPath = value; notifyPropertyChanged(); }
        }
        private string _keyFolderPath;

        public string DecryptionKeyPath {
            get { return _decryptionKeyPath; }
            set { _decryptionKeyPath = value; notifyPropertyChanged(); }
        }
        private string _decryptionKeyPath;

        public bool SkipHashVerification {
            get { return _skipHashVerification; }
            set { _skipHashVerification = value; notifyPropertyChanged(); }
        }
        private bool _skipHashVerification;

        public bool SkipSignatureVerification {
            get { return _skipSignatureVerification; }
            set { _skipSignatureVerification = value; notifyPropertyChanged(); notifyPropertyChanged(nameof(KeyFolderPath)); }
        }
        private bool _skipSignatureVerification;

        public string Error {
            get {
                throw new NotImplementedException();
            }
        }

        public bool Validate() {
            return (from p in new string[] {
                nameof(Drive), nameof(Partition), nameof(KeyFolderPath), nameof(DecryptionKeyPath)
                } select this[p]).All(s => s == string.Empty);
        }

        public SRFS.Model.FileSystem OpenFileSystem(SecureString decryptionPassword) {

            CngKey decryptionKey = null;

            try {
                decryptionKey = KeyUtility.LoadPrivateKey(_decryptionKeyPath, decryptionPassword);
            } catch (FileNotFoundException e) {
                throw new MessageException("The decryption key file could not be found.", "File Not Found", e);
            } catch (ArgumentException e) {
                throw new MessageException("The decryption key file does not contain a private key.", "Key Not Found", e);
            } catch (Exception e) {
                throw new MessageException("Invalid password or corrupt decryption key file.", "Key Load Failed", e);
            }

            Configuration.CryptoSettings = new CryptoSettings(decryptionKey, null, null);
            Configuration.Options = (SkipHashVerification ? Options.DoNotVerifyClusterHashes : Options.None) |
                (SkipSignatureVerification ? Options.DoNotVerifyClusterSignatures : Options.None);

            return FileSystem.Mount(Partition);
        }

        public string this[string columnName] {
            get {
                switch (columnName) {
                    case nameof(Drive):
                        if (_drive == null) return "Select a drive.";
                        break;
                    case nameof(Partition):
                        if (_partition == null) return _drive == null ? "Select a drive, then a partition." : "Select a partition.";
                        break;
                    case nameof(KeyFolderPath):
                        if (string.IsNullOrEmpty(_keyFolderPath) && !SkipSignatureVerification)
                            return "Enter a path to the folder containing public keys to verify signatures, or select 'Skip Signature Verification' below.";
                        break;
                    case nameof(DecryptionKeyPath):
                        if (string.IsNullOrEmpty(_decryptionKeyPath)) return "Enter the path to your decryption key.";
                        break;
                }
                return "";
            }
        }

        private void notifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
