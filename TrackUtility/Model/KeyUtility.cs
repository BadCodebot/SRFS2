using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security;
using System.IO;
using System.Runtime.InteropServices;

namespace TrackUtility.Model {
    class KeyUtility {

        private const int ECPrivateBlobLength = 206;
        private const int EncryptedECPrivateBlobLength = 208;
        private const int ECPublicBlobLength = 140;

        public static CngKey LoadPrivateKey(string path, SecureString password) {
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

        public static CngKey LoadPublicKey(string path) {
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
    }
}
