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

    public interface IOptionParser {

        bool MatchLongForm(object command, string optionName, string optionValue, LinkedList<string> args);

        bool MatchShortForm(object command, char optionName, string optionvalue, LinkedList<string> args);

        void Verify(object command);

        IEnumerable<OptionAttribute> OptionsParsed { get; }
    }
}
