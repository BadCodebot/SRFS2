using System;
using System.Security.Cryptography;
using System.IO;

namespace SRFS.Model.Data {

    public class KeyThumbprint {

        #region Constructor

        public KeyThumbprint() {
            _bytes = new byte[Length];
        }

        public KeyThumbprint(byte[] bytes, int offset = 0) :this() {
            if (bytes == null) throw new ArgumentNullException();
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + Length > bytes.Length) throw new ArgumentException();

            Buffer.BlockCopy(bytes, offset, _bytes, 0, Length);
        }

        public override string ToString() {
            return Convert.ToBase64String(_bytes);
        }

        public int CompareTo(KeyThumbprint other) {
            for (int i = 0; i < _bytes.Length; i++) {
                if (_bytes[i] < other._bytes[i]) return -1;
                else if (_bytes[i] > other._bytes[i]) return 1;
            }
            return 0;
        }

        public override int GetHashCode() {
            return BitConverter.ToInt32(_bytes, 0);
        }

        public override bool Equals(object obj) {
            KeyThumbprint t = obj as KeyThumbprint;
            if (t == null) return false;
            return CompareTo(t) == 0;
        }

        #endregion
        #region Properties

        public KeyThumbprint(CngKey key) : this() {
            using (SHA256Cng hasher = new SHA256Cng()) {
                byte[] keyBytes = key.Export(CngKeyBlobFormat.EccPublicBlob);
                hasher.TransformFinalBlock(keyBytes, 0, keyBytes.Length);
                Buffer.BlockCopy(hasher.Hash, 0, Bytes, 0, Length);
            }
        }

        #endregion
        #region Properties

        public byte[] Bytes => _bytes;

        #region Methods

        #endregion

        public const int Length = 32;
        private byte[] _bytes;

        #endregion
    }

    public static class KeyThumbprintExtensions {
        public static void Write(this BinaryWriter writer, KeyThumbprint thumbprint) => writer.Write(thumbprint.Bytes, 0, KeyThumbprint.Length);

        public static KeyThumbprint ReadKeyThumbprint(this BinaryReader reader) => new KeyThumbprint(reader.ReadBytes(KeyThumbprint.Length));
    }
}
