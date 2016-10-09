using Blocks.CommandLine;
using SRFS.IO;
using SRFS.Model;
using System;
using System.ComponentModel;
using SRFS.Model.Clusters;
using System.Security.Cryptography;
using System.Security;
using System.Text;

namespace SRFS.Commands {

    public enum KeyFileType : int {
        Public,
        Private
    }

    public class CryptoSettingsOptions {

        [Parameter(ShortForm = 'D', LongForm = "decryptionKey", Description = "Decryption Key File", IsRequired = true)]
        public string DecryptionKeyFile { get; private set; }

        [Parameter(ShortForm = 'E', LongForm = "encryptionKey", Description = "Encryption Key File", IsRequired = true)]
        public string EncryptionKeyFile { get; private set; }

        [Parameter(ShortForm = 'S', LongForm = "signatureKey", Description = "Signature Key File", IsRequired = true)]
        public string SigningKeyFile { get; private set; }

        private const int ECPrivateBlobLength = 206;
        private const int EncryptedECPrivateBlobLength = 208;
        private const int ECPublicBlobLength = 140;

        public string GetPassword() {
            var pwd = new StringBuilder();
            while (true) {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter) {
                    break;
                } else if (i.Key == ConsoleKey.Backspace) {
                    if (pwd.Length > 0) {
                        pwd.Remove(pwd.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                } else {
                    pwd.Append(i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd.ToString();
        }

        public byte[] HashString(string s) {
            using (var hasher = new SHA256Cng()) {
                byte[] bytes = Encoding.Unicode.GetBytes(s);
                hasher.TransformFinalBlock(bytes, 0, bytes.Length);
                return hasher.Hash;
            }
        }

        public CngKey GetDecryptionKey() {
            byte[] keyData = null;
            KeyFileType type;

            using (var file = new System.IO.FileStream(DecryptionKeyFile, System.IO.FileMode.Open))
            using (var reader = new System.IO.BinaryReader(file)) {
                type = (KeyFileType)reader.ReadUInt32();
                if (type != KeyFileType.Private) throw new CommandLineArgumentException("Decryption Key File must contain a private key");
                keyData = reader.ReadBytes(EncryptedECPrivateBlobLength);
            }

            Console.Write("Enter Decryption Key Password: ");
            string p1 = Console.ReadLine();

            using (AesCng aes = new AesCng()) {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = HashString(p1);
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

        public CngKey GetSigningKey() {
            byte[] keyData = null;
            KeyFileType type;

            using (var file = new System.IO.FileStream(SigningKeyFile, System.IO.FileMode.Open))
            using (var reader = new System.IO.BinaryReader(file)) {
                type = (KeyFileType)reader.ReadUInt32();
                if (type != KeyFileType.Private) throw new CommandLineArgumentException("Signing Key File must contain a private key");
                keyData = reader.ReadBytes(EncryptedECPrivateBlobLength);
            }

            Console.Write("Enter Signing Key Password: ");
            string p1 = Console.ReadLine();

            using (AesCng aes = new AesCng()) {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = HashString(p1);
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

        public CngKey GetEncryptionKey() {
            byte[] keyData = null;
            KeyFileType type;

            using (var file = new System.IO.FileStream(EncryptionKeyFile, System.IO.FileMode.Open))
            using (var reader = new System.IO.BinaryReader(file)) {
                type = (KeyFileType)reader.ReadUInt32();
                if (type != KeyFileType.Public) throw new CommandLineArgumentException("Encryption Key File must contain a public key");
                keyData = reader.ReadBytes(ECPublicBlobLength);
            }

            return CngKey.Import(keyData, CngKeyBlobFormat.EccPublicBlob);
        }
    }
}
