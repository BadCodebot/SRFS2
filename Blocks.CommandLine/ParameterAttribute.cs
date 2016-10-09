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
    public class ParameterAttribute : OptionAttribute {

        public string Converter { get; set; } = null;

        public bool IsRequired { get; set; } = false;

        public string OptionalArgument { get; set; } = null;

        public string Type {
            get {
                return _type;
            }
            set {
                _type = value == string.Empty ? null : value;
            }
        }
        private string _type = null;

        public string EnvironmentalVariable {
            get {
                return _environmentalVariable;
            }
            set {
                _environmentalVariable = value == string.Empty ? null : value;
            }
        }
        private string _environmentalVariable = null;

        public override IOptionParser CreateParser(PropertyInfo property) { return new ParameterParser(property, this); }

        public bool Used { get; private set; } = false;
    }
}
