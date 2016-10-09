using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blocks.CommandLine {

    [AttributeUsage(AttributeTargets.Method)]
    public class ArgumentsVerificationAttribute : Attribute {
    }
}
