using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blocks.CommandLine {
    public class Usage {

        public string GetUsage(Type commandType, string invocation) {
            CommandLineParser parser = new CommandLineParser(commandType);
            StringBuilder usage = new StringBuilder();

            usage.Append($"Usage: {invocation}");

            var options = parser.Options.ToList();

            if (options.Count > 1) usage.Append(" [OPTIONS]");
            else if (options.Count == 1) usage.Append(" [OPTION]");

            if (parser.Arguments != null) {
                // Still need to verify the attribute, min < max, etc.
                string type = parser.Arguments.TypeDescription == null ? "OPERAND" : parser.Arguments.TypeDescription;
                if (parser.Arguments.Minimum == 0) {
                    if (parser.Arguments.Maximum > 1) usage.Append($" [{type}...]");
                    else usage.Append($" [{type}]");
                } else {
                    if (parser.Arguments.Maximum > 1) usage.Append($" {type}...");
                    else usage.Append($" {type}");
                }
            }
            usage.AppendLine();

            if (options.Count > 0) usage.AppendLine($"Options:");
            foreach (var o in options) usage.Append($"  {GetOptionUsage(o)}");

            return usage.ToString();
        }

        public string GetOptionUsage(OptionAttribute option) {
            StringBuilder usage = new StringBuilder();

            StringBuilder column1 = new StringBuilder();

            if (option.HasShortForm) column1.Append($"-{option.ShortForm}");
            if (option.HasShortForm && option.HasLongForm) column1.Append(", ");
            if (option.HasLongForm) {
                column1.Append($"--{option.LongForm}");
                if (option is ParameterAttribute) {
                    ParameterAttribute p = (ParameterAttribute)option;
                    if (p.OptionalArgument == null) column1.Append($"={(p.Type != null ? p.Type : "VALUE")}");
                    else column1.Append($"[={(p.Type != null ? p.Type : "VALUE")}]");
                }
            }
            usage.Append(column1.ToString().PadRight(OptionHelpColumnWidth));

            usage.Append($"  {option.Description}");

            if (option is ParameterAttribute && ((ParameterAttribute)option).IsRequired) usage.Append(" (Required)");

            usage.AppendLine();
            return usage.ToString();
        }

        public const int DEFAULT_COMMAND_HELP_COLUMN_WIDTH = 15;
        public const int DEFAULT_OPTION_HELP_COLUMN_WIDTH = 25;

        public int CommandHelpColumnWidth { get; set; } = DEFAULT_COMMAND_HELP_COLUMN_WIDTH;
        public int OptionHelpColumnWidth { get; set; } = DEFAULT_OPTION_HELP_COLUMN_WIDTH;
    }
}
