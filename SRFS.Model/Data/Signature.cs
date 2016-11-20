using System;
using System.Security.Cryptography;
using System.IO;

namespace SRFS.Model.Data {

    public class Signature {

        #region Constructor

        public Signature(CngKey key, byte[] data, int offset, int count) : this() {
            using (var dsa = new ECDsaCng(key)) {
                Buffer.BlockCopy(dsa.SignData(data, offset, count), 0, Bytes, 0, Length);
            }
        }

        public Signature() {
            _bytes = new byte[Length];
        }

        public Signature(byte[] bytes, int offset = 0) :this() {
            if (bytes == null) throw new ArgumentNullException();
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + Length > bytes.Length) throw new ArgumentException();

            Buffer.BlockCopy(bytes, offset, _bytes, 0, Length);
        }

        public override string ToString() {
            return Convert.ToBase64String(_bytes);
        }


        #endregion
        #region Properties

        public const int Length = 132;

        public byte[] Bytes => _bytes;

        #endregion
        #region Methods

        public int CompareTo(Signature other) {
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
            Signature t = obj as Signature;
            if (t == null) return false;
            return CompareTo(t) == 0;
        }

        public bool Verify(byte[] data, int offset, int count, CngKey key) {
            using (var dsa = new ECDsaCng(key)) {
                return dsa.VerifyData(data, offset, count, Bytes);
            }
        }

        #endregion

        private byte[] _bytes;
    }

    public static class SignatureExtensions {

        public static Signature ReadSignature(this BinaryReader reader) {
            return new Signature(reader.ReadBytes(Signature.Length));
        }
    }
}
