using Blocks.CommandLine;
using SRFS.IO;
using SRFS.Model;
using System;
using System.ComponentModel;
using SRFS.Model.Clusters;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using SRFS.Model.Data;

namespace SRFS.Commands {

    [Command(Name = "makekey", Description = "Make an EC public/private key pair")]
    public class MakeKeyCommand {

        public enum FileType : int {
            Public,
            Private
        }

        [Parameter(ShortForm = 'v', LongForm = "private", Description = "Private Key Output File", IsRequired = true)]
        public string PrivateKeyFile { get; private set; }

        [Parameter(ShortForm = 'p', LongForm = "public", Description = "Public Key Output File", IsRequired = true)]
        public string PublicKeyFile { get; private set; }

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

        private const int ECPrivateBlobLength = 206;
        private const int EncryptedECPrivateBlobLength = 208;
        private const int ECPublicBlobLength = 140;

        private byte[] IV = new byte[16];

        [Invoke]
        public void Invoke() {

            using (AesCng aes = new AesCng()) {

                Console.Write("Enter Key Password: ");
                string p1 = Console.ReadLine();
                Console.Write("Confirm Password: ");
                string p2 = Console.ReadLine();
                if (!p1.Equals(p2)) {
                    Console.Error.WriteLine("Passwords did not match.");
                    return;
                }

                CngKey pk = CngKey.Create(CngAlgorithm.ECDiffieHellmanP521, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport });
                byte[] publicKeyBlock = pk.Export(CngKeyBlobFormat.EccPublicBlob);

                byte[] privateKeyBlob = pk.Export(CngKeyBlobFormat.EccPrivateBlob);

                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.IV = IV;
                aes.Key = HashString(p1);

                byte[] decryptedECPrivateBlob = new byte[EncryptedECPrivateBlobLength];
                Buffer.BlockCopy(privateKeyBlob, 0, decryptedECPrivateBlob, 0, privateKeyBlob.Length);

                byte[] encryptedData = null;
                using (var encryptor = aes.CreateEncryptor()) {
                    encryptedData = encryptor.TransformFinalBlock(decryptedECPrivateBlob, 0, decryptedECPrivateBlob.Length);
                }

                using (var file = new System.IO.FileStream(PublicKeyFile, System.IO.FileMode.CreateNew))
                using (var writer = new System.IO.BinaryWriter(file)) {
                    writer.Write((int)FileType.Public);
                    file.Write(publicKeyBlock, 0, publicKeyBlock.Length);
                }

                using (var file = new System.IO.FileStream(PrivateKeyFile, System.IO.FileMode.CreateNew))
                using (var writer = new System.IO.BinaryWriter(file)) {
                    writer.Write((int)FileType.Private);
                    writer.Write(encryptedData, 0, encryptedData.Length);
                }

                Console.WriteLine($"Thumbprint: {new KeyThumbprint(pk)}");
            }
        }
    }
}
