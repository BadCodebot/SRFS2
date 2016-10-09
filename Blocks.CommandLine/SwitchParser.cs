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

    public class SwitchParser : IOptionParser {

        public SwitchParser(PropertyInfo info, SwitchAttribute attribute) {
            Property = info;
            Attribute = attribute;

            if (!Attribute.HasShortForm && !Attribute.HasLongForm) throw new NotSupportedException("Attribute must have a short form and/or a long form.");
            if (Property.PropertyType != typeof(bool)) throw new NotSupportedException("Switch attribute applied to non-bool type.");
        }

        public bool MatchLongForm(object command, string optionName, string optionValue, LinkedList<string> args) {
            if (Attribute.LongForm != optionName) return false;
            Property.SetValue(command, true);
            return true;
        }

        public bool MatchShortForm(object command, char optionName, string optionValue, LinkedList<string> args) {
            if (Attribute.ShortForm != optionName) return false;
            Property.SetValue(command, true);
            if (optionValue != null) args.AddFirst($"-{optionValue}");
            return true;
        }

        public void Verify(object command) { }

        public IEnumerable<OptionAttribute> OptionsParsed { get { yield return Attribute; } }

        private SwitchAttribute Attribute;
        private PropertyInfo Property;
    }
}
