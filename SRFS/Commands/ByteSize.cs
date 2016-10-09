using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SRFS.Commands {

    [TypeConverter(typeof(ByteSizeConverter))]
    public struct ByteSize {

        public ByteSize(long bytes) { Bytes = bytes; }

        public long Bytes { get; private set; }
    }
}
