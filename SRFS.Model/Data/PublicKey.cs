using System;
using System.Security.Cryptography;
using System.IO;

namespace SRFS.Model.Data {

    public class PublicKey {

        #region Constructor

        public PublicKey(byte[] bytes, int offset = 0) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (offset + Length > bytes.Length) throw new ArgumentException();

            _bytes = new byte[Length];
            Buffer.BlockCopy(bytes, offset, _bytes, 0, Length);

            _key = null;

            _thumbprint = null;
        }

        //public PublicKey(ECDiffieHellmanCng key) {
        //    if (key == null) throw new ArgumentNullException(nameof(key));

        //    _bytes = key.PublicKey.ToByteArray();

        //    _key = key;

        //    _thumbprint = null;
        //}

        #endregion
        #region Properties

        public const int Length = 140;

        public byte[] Bytes => _bytes;

        public CngKey Key {
            get {
                if (_key == null) _key = CngKey.Import(Bytes, CngKeyBlobFormat.EccPublicBlob);
                return _key;
            }
        }

        public KeyThumbprint Thumbprint {
            get {
                if (_thumbprint == null) _thumbprint = new KeyThumbprint(_bytes);
                return _thumbprint;
            }
        }

        #endregion
        #region Fields

        private byte[] _bytes = null;
        private CngKey _key = null;
        private KeyThumbprint _thumbprint = null;

        #endregion
    }

    public static class PublicKeyExtensions {

        public static PublicKey ReadPublicKey(this BinaryReader reader) {
            return new PublicKey(reader.ReadBytes(PublicKey.Length));
        }
    }
}
