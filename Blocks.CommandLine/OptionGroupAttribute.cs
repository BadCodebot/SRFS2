using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;

namespace Blocks.CommandLine {

    [AttributeUsage(AttributeTargets.Property)]
    public class OptionGroupAttribute : Attribute, IOptionParserFactory {

        public IOptionParser CreateParser(PropertyInfo property) { return new OptionGroupParser(property); }
    }
}
