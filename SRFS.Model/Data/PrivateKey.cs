using System;
using System.Security.Cryptography;

namespace SRFS.Model.Data {

    public class PrivateKey {

        #region Constructor

        public PrivateKey(CngKey key) {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _key = key;

            _thumbprint = null;
        }

        #endregion
        #region Properties

        public CngKey Key => _key;

        public KeyThumbprint Thumbprint {
            get {
                if (_thumbprint == null) _thumbprint = new KeyThumbprint(_key);
                return _thumbprint;
            }
        }

        #endregion
        #region Fields

        private CngKey _key = null;
        private KeyThumbprint _thumbprint = null;

        #endregion
    }
}
