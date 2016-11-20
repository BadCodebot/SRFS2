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
    public class MountFileSystemModel : IDataErrorInfo, INotifyPropertyChanged {

        private const int ECPrivateBlobLength = 206;
        private const int EncryptedECPrivateBlobLength = 208;
        private const int ECPublicBlobLength = 140;

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

        public string EncryptionKeyPath {
            get { return _encryptionKeyPath; }
            set { _encryptionKeyPath = value; notifyPropertyChanged(); }
        }
        private string _encryptionKeyPath;

        public string SigningKeyPath {
            get { return _signingKeyPath; }
            set { _signingKeyPath = value; notifyPropertyChanged(); }
        }
        private string _signingKeyPath;

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

        public bool UseSeparateSigningKey {
            get { return _useSeparateSignatureKey; }
            set {
                _useSeparateSignatureKey = value;
                notifyPropertyChanged();
                notifyPropertyChanged(nameof(SigningKeyPath));
                notifyPropertyChanged(nameof(UseSeparateSigningKey));
            }
        }
        private bool _useSeparateSignatureKey = false;


        public string Error {
            get {
                throw new NotImplementedException();
            }
        }

        public bool Validate() {
            return (from p in new string[] {
                nameof(Drive), nameof(Partition), nameof(KeyFolderPath), nameof(DecryptionKeyPath),
                nameof(EncryptionKeyPath), nameof(SigningKeyPath) } select this[p]).All(s => s == string.Empty);
        }

        private static CngKey loadPrivateKey(string path, SecureString password) {
            if (!new FileInfo(path).Exists) throw new FileNotFoundException();

            // We need to use the OS for all this

            byte[] keyData = null;
            KeyFileType type;

            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(file)) {
                type = (KeyFileType)reader.ReadUInt32();
                if (type != KeyFileType.Private) throw new ArgumentException();
                keyData = reader.ReadBytes(EncryptedECPrivateBlobLength);
            }

            using (AesCng aes = new AesCng()) {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = HashString(password);
                aes.IV = new byte[16];

                byte[] paddedDecryptedECPrivateBlob = new byte[ECPrivateBlobLength];
                byte[] decryptedECPrivateBlob = new byte[ECPrivateBlobLength];
                using (var decryptor = aes.CreateDecryptor()) {
                    paddedDecryptedECPrivateBlob = decryptor.TransformFinalBlock(keyData, 0, keyData.Length);
                    Buffer.BlockCopy(paddedDecryptedECPrivateBlob, 0, decryptedECPrivateBlob, 0, ECPrivateBlobLength);
                }

                return CngKey.Import(decryptedECPrivateBlob, CngKeyBlobFormat.EccPrivateBlob);
            }
        }

        public static CngKey loadPublicKey(string path) {
            if (!new FileInfo(path).Exists) throw new FileNotFoundException();

            byte[] keyData = null;
            KeyFileType type;

            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(file)) {
                type = (KeyFileType)reader.ReadUInt32();
                if (type != KeyFileType.Public) throw new ArgumentException();
                keyData = reader.ReadBytes(ECPublicBlobLength);
            }

            return CngKey.Import(keyData, CngKeyBlobFormat.EccPublicBlob);
        }

        public SRFS.Model.FileSystem OpenFileSystem(SecureString decryptionPassword, SecureString signingPassword) {

            CngKey decryptionKey = null;
            CngKey signingKey = null;
            CngKey encryptionKey = null;

            try {
                decryptionKey = loadPrivateKey(_decryptionKeyPath, decryptionPassword);
            } catch (FileNotFoundException e) {
                throw new MessageException("The decryption key file could not be found.", "File Not Found", e);
            } catch (ArgumentException e) {
                throw new MessageException("The decryption key file does not contain a private key.", "Key Not Found", e);
            } catch (Exception e) {
                throw new MessageException("Invalid password or corrupt decryption key file.", "Key Load Failed", e);
            }

            if (UseSeparateSigningKey) {
                try {
                    signingKey = loadPrivateKey(_signingKeyPath, signingPassword);
                } catch (FileNotFoundException e) {
                    throw new MessageException("The signing key file could not be found.", "File Not Found", e);
                } catch (ArgumentException e) {
                    throw new MessageException("The signing key file does not contain a private key.", "Key Not Found", e);
                } catch (Exception e) {
                    throw new MessageException("Invalid password or corrupt signing key file.", "Key Load Failed", e);
                }
            } else {
                signingKey = decryptionKey;
            }

            try {
                encryptionKey = loadPublicKey(_encryptionKeyPath);
            } catch (FileNotFoundException e) {
                throw new MessageException("The encryption key file could not be found.", "File Not Found", e);
            } catch (ArgumentException e) {
                throw new MessageException("The encryption key file does not contain a public key.", "Key Not Found", e);
            } catch (Exception e) {
                throw new MessageException("Corrupt encryption key file.", "Key Load Failed", e);
            }

            Configuration.CryptoSettings = new CryptoSettings(decryptionKey, signingKey, encryptionKey);
            Configuration.Options = (SkipHashVerification ? Options.DoNotVerifyClusterHashes : Options.None) |
                (SkipSignatureVerification ? Options.DoNotVerifyClusterSignatures : Options.None);

            return FileSystem.Mount(Partition);
        }

        private static byte[] HashString(SecureString s) {
            // Until we have some better way to securely handle generating hashes in unmanaged memory...
            // Of course the resulting byte[] needs to be handled securely as well, but who knows what SHA256Cng is doing?
            IntPtr sPointer = Marshal.SecureStringToGlobalAllocUnicode(s);
            try {
                IntPtr p = sPointer;
                byte[] bytes = new byte[2];
                using (var hasher = new SHA256Cng()) {
                    for (int i = 0; i < s.Length - 1; i++) {
                        Marshal.Copy(p, bytes, 0, 2);
                        hasher.TransformBlock(bytes, 0, 2, null, 0);
                        p = IntPtr.Add(p, 2);
                    }
                    Marshal.Copy(p, bytes, 0, 2);
                    hasher.TransformFinalBlock(bytes, 0, 2);
                    return hasher.Hash;
                }
            } finally {
                Marshal.ZeroFreeGlobalAllocUnicode(sPointer);
            }
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
                    case nameof(EncryptionKeyPath):
                        if (string.IsNullOrEmpty(_encryptionKeyPath)) return "Enter the path to your encryption key.";
                        break;
                    case nameof(DecryptionKeyPath):
                        if (string.IsNullOrEmpty(_decryptionKeyPath)) return "Enter the path to your decryption key.";
                        break;
                    case nameof(SigningKeyPath):
                        if (UseSeparateSigningKey && string.IsNullOrEmpty(_signingKeyPath)) return "Enter the path to your signing key.";
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
