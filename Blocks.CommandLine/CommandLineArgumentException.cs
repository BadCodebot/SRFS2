using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blocks.CommandLine {

    public class CommandLineArgumentException : Exception {
        public CommandLineArgumentException(string s) : base(s) { }
    }
}
