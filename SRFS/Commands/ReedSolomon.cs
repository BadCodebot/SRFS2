using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SRFS.Commands {

    [TypeConverter(typeof(ReedSolomonConverter))]
    public struct ReedSolomon {

        public ReedSolomon(int n, int k) { N = n; K = k; }

        public int N { get; private set; }
        public int K { get; private set; }
    }
}
