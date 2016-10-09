using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;

namespace Blocks.CommandLine {

    internal class Arguments {

        public Arguments(PropertyInfo info, ArgumentsAttribute a) {
            Info = info;
            Attribute = a;

            if (!typeof(ICollection<string>).IsAssignableFrom(Info.PropertyType))
                throw new NotSupportedException("Arguments Attribute property type is not assignable to ICollection<string>.");
            if (Attribute.Maximum < -1 || Attribute.Minimum < 0 || (Attribute.Maximum >= 0 && Attribute.Maximum < Attribute.Minimum))
                throw new NotSupportedException("Invalid minimum and/or maximum for Arguments attribute.");
        }

        public void Add(object command, string s) => ((ICollection<string>)Info.GetValue(command)).Add(s);

        public int Count(object command) => ((ICollection<string>)Info.GetValue(command)).Count;

        private PropertyInfo Info;
        public ArgumentsAttribute Attribute { get; set; }
    }
}
