using System;

namespace SRFS.Model.Data {

    public class ByteArray<T> : IComparable<T> where T : ByteArray<T> {

        #region Constructor

        protected ByteArray(byte[] bytes, int offset, int length) {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (offset + length > bytes.Length) throw new ArgumentException();

            _bytes = new byte[length];
            Buffer.BlockCopy(bytes, 0, _bytes, offset, length);
        }

        public override string ToString() {
            return Convert.ToBase64String(_bytes);
        }

        public int CompareTo(T other) {
            for (int i = 0; i < _bytes.Length; i++) {
                if (_bytes[i] < other._bytes[i]) return -1;
                else if (_bytes[i] > other._bytes[i]) return 1;
            }
            return 0;
        }

        public override int GetHashCode() {
            if (_bytes.Length >= sizeof(int)) return BitConverter.ToInt32(_bytes, 0);
            if (_bytes.Length >= sizeof(short)) return BitConverter.ToInt16(_bytes, 0);
            return _bytes[0];
        }

        public override bool Equals(object obj) {
            T t = obj as T;
            if (t == null) return false;
            return CompareTo(t) == 0;
        }

        #endregion
        #region Properties

        public byte[] Bytes => _bytes;
        private byte[] _bytes;

        #endregion
    }
}
