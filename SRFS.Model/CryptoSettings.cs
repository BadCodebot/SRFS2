using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using SRFS.Model.Data;

namespace SRFS.Model {

    public class CryptoSettings {

        public CryptoSettings(CngKey decryptionKey, CngKey signingKey, CngKey encryptionKey) {
            DecryptionKey = decryptionKey;
            SigningKey = signingKey;
            EncryptionKey = encryptionKey;

            _signingKeyThumbprint = new Lazy<KeyThumbprint>(calculateSigningKeyThumbprint);
            _decryptionKeyThumbprint = new Lazy<KeyThumbprint>(calculateDecryptionKeyThumbprint);
            _encryptionKeyThumbprint = new Lazy<KeyThumbprint>(calculateEncryptionKeyThumbprint);
        }

        public CngKey DecryptionKey { get; private set; }
        public KeyThumbprint DecryptionKeyThumbprint => _decryptionKeyThumbprint.Value;
        private Lazy<KeyThumbprint> _decryptionKeyThumbprint;
        private KeyThumbprint calculateDecryptionKeyThumbprint() => new KeyThumbprint(DecryptionKey);

        public CngKey EncryptionKey { get; private set; }
        public KeyThumbprint EncryptionKeyThumbprint => _encryptionKeyThumbprint.Value;
        private Lazy<KeyThumbprint> _encryptionKeyThumbprint;
        private KeyThumbprint calculateEncryptionKeyThumbprint() => new KeyThumbprint(EncryptionKey);

        public CngKey SigningKey { get; private set; }
        public KeyThumbprint SigningKeyThumbprint => _signingKeyThumbprint.Value;
        private Lazy<KeyThumbprint> _signingKeyThumbprint;
        private KeyThumbprint calculateSigningKeyThumbprint() => new KeyThumbprint(SigningKey);
    }
}
