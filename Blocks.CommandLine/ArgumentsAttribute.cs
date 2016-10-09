using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Blocks.CommandLine {

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentsAttribute : Attribute {

        public int Minimum { get; set; } = 0;
        public int Maximum { get; set; } = -1;

        public string TypeDescription { get; set; } = null;
    }
}
