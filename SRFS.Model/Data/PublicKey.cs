using System;
using System.Security.Cryptography;

namespace SRFS.Model.Data {

    public class PublicKey : ByteArray<PublicKey> {

        #region Constructor

        public PublicKey() : base(new byte[Length], 0, Length) { }


        public PublicKey(byte[] bytes, int offset = 0) : base(bytes, offset, Length) {
            if (bytes.Length != Length) throw new ArgumentException();
        }

        public PublicKey(ECDiffieHellmanCng key) : this(key.PublicKey.ToByteArray()) { }

        #endregion
        #region Properties

        public const int Length = 140;

        #endregion
        #region Methods

        public CngKey GetCngKey() => CngKey.Import(Bytes, CngKeyBlobFormat.EccPublicBlob);

        #endregion
    }
}
