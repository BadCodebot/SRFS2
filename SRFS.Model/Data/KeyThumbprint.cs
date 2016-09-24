using System;
using System.Security.Cryptography;

namespace SRFS.Model.Data {

    public class KeyThumbprint : ByteArray<KeyThumbprint> {

        #region Constructor

        public KeyThumbprint() : base(new byte[Length], 0, Length) { }

        public KeyThumbprint(byte[] bytes, int offset = 0) : base(bytes, offset, Length) { }

        public KeyThumbprint(CngKey key) : this() {
            using (SHA256Cng hasher = new SHA256Cng()) {
                byte[] keyBytes = key.Export(CngKeyBlobFormat.EccPublicBlob);
                hasher.TransformFinalBlock(keyBytes, 0, keyBytes.Length);
                Buffer.BlockCopy(hasher.Hash, 0, Bytes, 0, Length);
            }
        }

        #endregion
        #region Properties

        public const int Length = 32;

        #endregion
    }
}
