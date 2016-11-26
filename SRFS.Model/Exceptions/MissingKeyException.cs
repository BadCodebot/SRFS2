using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRFS.Model.Data;

namespace SRFS.Model.Exceptions {
    public class MissingKeyException : Exception {

        public MissingKeyException(KeyThumbprint thumbprint) {
            _thumbprint = thumbprint;
        }

        public KeyThumbprint KeyThumbprint => _thumbprint;
        private KeyThumbprint _thumbprint;
    }
}
