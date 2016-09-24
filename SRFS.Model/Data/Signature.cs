using System;
using System.Security.Cryptography;

namespace SRFS.Model.Data {

    public class Signature : ByteArray<Signature> {

        #region Constructor

        public Signature() : base(new byte[Length], 0, Length) { }

        public Signature(byte[] bytes, int offset = 0) : base(bytes, offset, Length) {
            if (bytes.Length != Length) throw new ArgumentException($"Unexpected signature size: {bytes.Length} (expected {Length}).");
        }

        public Signature(CngKey key, byte[] data, int offset, int count) : this() {
            using (var dsa = new ECDsaCng(key)) {
                Buffer.BlockCopy(dsa.SignData(data, offset, count), 0, Bytes, 0, Length);
            }
        }

        #endregion
        #region Properties

        public const int Length = 132;

        #endregion
        #region Methods

        public bool Verify(byte[] data, int offset, int count, CngKey key) {
            using (var dsa = new ECDsaCng(key)) {
                return dsa.VerifyData(data, offset, count, Bytes);
            }
        }

        #endregion
    }
}
