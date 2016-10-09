using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ComponentModel;
using System.Collections;

namespace Blocks.CommandLine {

    public class CommandLineProgram {

        [Parameter(ShortForm = 'h', LongForm = "help", Description = "Show usage help.", Type = "COMMAND", OptionalArgument = nameof(HelpSelected))]
        public string HelpCommand { get; set; } = null;
        public bool HelpSelected { get; set; } = false;

        [Switch(LongForm = "version", Description = "Show version number.")]
        public bool VersionSelected { get; set; } = false;

        [Arguments()]
        public LinkedList<string> Arguments { get; private set; } = new LinkedList<string>();

        public CommandLineProgram(string programInvocation, IEnumerable<Type> commands = null) {
            _programInvocation = programInvocation;
            if (commands != null) _commandTypes.AddRange(commands);
        }

        public Version Version {
            get {
                return _version;
            }
            set {
                _version = value;
            }
        }
        private Version _version = new Version();

        private string _programInvocation;

        public int Execute(IEnumerable<string> args) {
            try {
                CommandLineParser p = new CommandLineParser(GetType(), CommandLineParserOptions.END_ON_NONOPTION);
                p.Parse(this, args);
            } catch (CommandLineArgumentException) {
                if (!HelpSelected) throw;
            }

            if (HelpSelected) {
                if (HelpCommand == null && Arguments.Count == 0) {
                    Console.Out.Write(GetUsage());
                } else {
                    if (HelpCommand == null) {
                        HelpCommand = Arguments.First();
                        Arguments.RemoveFirst();
                    }
                    var c = (
                        from y in
                            from type in CommandTypes
                            select new { Type = type, Name = type.GetCustomAttribute<CommandAttribute>().Name }
                        where y.Name == HelpCommand
                        select y).FirstOrDefault();
                    if (c == null) throw new CommandLineArgumentException($"Unknown command: {HelpCommand}");
                    Console.Out.Write(_usage.GetUsage(c.Type, $"{_programInvocation} {c.Name}"));
                }
                return 0;
            } else if (VersionSelected) {
                Console.WriteLine(Version);
                return 0;
            } else {
                if (Arguments.Count == 0) throw new CommandLineArgumentException("No command specified");
                string commandName = Arguments.First();
                Arguments.RemoveFirst();

                try {
                    foreach (Type commandType in _commandTypes) {
                        CommandAttribute commandAttribute = commandType.GetCustomAttribute<CommandAttribute>();
                        if (commandAttribute != null && commandAttribute.Name == commandName) {
                            object command = CreateCommand(commandType);
                            CommandLineParser parser = new CommandLineParser(commandType);
                            parser.Parse(command, Arguments);
                            return InvokeCommand(command);
                        }
                    }

                    throw new CommandLineArgumentException($"Unknown command: {commandName}");
                } catch (TargetInvocationException) {
                    throw;
                }
            }
        }

        protected virtual object CreateCommand(Type commandType) {
            return Activator.CreateInstance(commandType);
        }

        protected virtual int InvokeCommand(object command) {
            MethodInfo m = (from method in command.GetType().GetMethods() where method.GetCustomAttribute<InvokeAttribute>() != null select method).SingleOrDefault();
            if (m == null) throw new NotSupportedException("No invocable method found for command.");
            if (m.ReturnType == typeof(void)) {
                m.Invoke(command, new object[0]);
                return 0;
            } else if (m.ReturnType == typeof(int)) {
                return (int)(m.Invoke(command, new object[0]));
            } else {
                throw new NotSupportedException("Unsupported return type for invocable method.");
            }
        }

        protected virtual string GetUsage() {
            StringBuilder usage = new StringBuilder();
            usage.AppendLine($"Usage: {_programInvocation} [Options] COMMAND [Command Options] [Arguments]");
            usage.AppendLine($"Options:");
            CommandLineParser p = new CommandLineParser(GetType(), CommandLineParserOptions.END_ON_NONOPTION);
            foreach (var o in p.Options) usage.Append($"  {_usage.GetOptionUsage(o)}");
            usage.AppendLine();
            usage.AppendLine("Commands:");

            int width = Math.Max(
                (from c in CommandTypes select c.GetCustomAttribute<CommandAttribute>().Name.Length).Max(), CommandHelpColumnWidth);

            foreach (var commandAttribute in
                from a in
                    from c in CommandTypes
                    select c.GetCustomAttribute<CommandAttribute>()
                orderby a.Name
                select a) {
                usage.AppendLine($"  {commandAttribute.Name.PadRight(width)}  {commandAttribute.Description}");
            }

            foreach (var commandInfo in
                from x in
                    from t in CommandTypes
                    select new { Type = t, Attribute = t.GetCustomAttribute<CommandAttribute>() }
                orderby x.Attribute.Name
                select x) {
                usage.AppendLine();
                usage.Append($"{commandInfo.Attribute.Name} ");
                usage.Append(_usage.GetUsage(commandInfo.Type, $"{_programInvocation} {commandInfo.Attribute.Name}"));
            }

            return usage.ToString();
        }

        public int CommandHelpColumnWidth {
            get {
                return _usage.CommandHelpColumnWidth;
            }
            set {
                _usage.CommandHelpColumnWidth = value;
            }
        }
        public int OptionHelpColumnWidth {
            get {
                return _usage.OptionHelpColumnWidth;
            }
            set {
                _usage.OptionHelpColumnWidth = value;
            }
        }

        private Usage _usage = new Usage();

        public IList<Type> CommandTypes => _commandTypes;
        private List<Type> _commandTypes = new List<Type>();
    }
}
