using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Blocks.CommandLine {

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute {

        public string Name { get; set; } = null;
        public string Description { get; set; } = null;
    }
}
