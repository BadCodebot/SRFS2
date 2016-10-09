using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Blocks.CommandLine {

    [Flags]
    public enum CommandLineParserOptions {
        NONE = 0x00,
        END_ON_NONOPTION = 0x01
    }

    public class CommandLineParser {

        #region Constructors

        public CommandLineParser(Type commandType, CommandLineParserOptions parserOptions = CommandLineParserOptions.NONE) {
            _parserOptions = parserOptions;

            _options = new OptionGroupParser(commandType);

            _arguments = (from x in
                              from p in commandType.GetProperties()
                              select new { Property = p, Attribute = p.GetCustomAttribute<ArgumentsAttribute>() }
                          where x.Attribute != null
                          select new Arguments(x.Property, x.Attribute)).FirstOrDefault();

            _argumentsVerification = (from m in commandType.GetMethods()
                                      from a in m.GetCustomAttributes<ArgumentsVerificationAttribute>()
                                      select m).SingleOrDefault();
            if (_argumentsVerification != null) {
                // Check the return and parameter types
                if (_argumentsVerification.ReturnType != typeof(void)) throw new NotSupportedException("Arguments Verification method does not return void.");
                if (_argumentsVerification.GetParameters().Length != 0) throw new NotSupportedException("Arguments Verification method does not take parameters.");
            }
        }

        #endregion

        public void Parse(object command, IEnumerable<string> args) {

            LinkedList<string> argsList = new LinkedList<string>(args);

            try {
                while (argsList.Count != 0) {
                    string option = argsList.First.Value;
                    argsList.RemoveFirst();

                    if (option.StartsWith("--") && option.Length > 2) {
                        processLongForm(command, option, argsList);
                    } else if (option.StartsWith("-") && option.Length > 1) {
                        processShortForm(command, option, argsList);
                    } else if (option == "--") {
                        addAllOperands(command, argsList);
                    } else {
                        if (_arguments == null) throw new CommandLineArgumentException("Unexpected argument on command line");
                        _arguments.Add(command, option);
                        if ((_parserOptions & CommandLineParserOptions.END_ON_NONOPTION) != 0) addAllOperands(command, argsList);
                    }
                }
            } catch (FormatException e) {
                throw new CommandLineArgumentException(e.Message);
            }

            _options.Verify(command);

            if (_arguments != null) {
                // If a verify method was specified, call it.
                if (_argumentsVerification != null) {
                    _argumentsVerification.Invoke(command, new object[0]);
                } else {
                    if (_arguments.Count(command) < _arguments.Attribute.Minimum)
                        throw new CommandLineArgumentException($"Command argument{(_arguments.Attribute.Minimum > 1 ? "s" : "")} missing.");
                    if (_arguments.Attribute.Maximum != -1 && _arguments.Count(command) > _arguments.Attribute.Maximum) {
                        if (_arguments.Attribute.Maximum == 0)
                            throw new CommandLineArgumentException($"Unexpected argument{(_arguments.Count(command) > 1 ? "s" : "")} for command.");
                        else
                            throw new CommandLineArgumentException($"Too many arguments for command.");
                    }
                }
            }
        }

        private void processLongForm(object command, string option, LinkedList<string> argsList) {
            int indexOfEquals = option.IndexOf('=');
            if (indexOfEquals == 2) throw new CommandLineArgumentException("Missing option name.");

            string optionName = indexOfEquals == -1 ? option.Substring(2) : option.Substring(2, indexOfEquals - 2);
            string optionValue = indexOfEquals == -1 ? null : option.Substring(indexOfEquals + 1);
            optionValue = optionValue == "" ? null : optionValue;

            if (!_options.MatchLongForm(command, optionName, optionValue, argsList)) throw new CommandLineArgumentException($"Unknown option {option}.");
        }

        private void processShortForm(object command, string option, LinkedList<string> argsList) {
            string optionValue = option.Substring(2);
            optionValue = optionValue == "" ? null : optionValue;
            if (!_options.MatchShortForm(command, option[1], optionValue, argsList)) throw new CommandLineArgumentException($"Unknown option {option}.");
        }

        private void addAllOperands(object command, LinkedList<string> argsList) {
            if (_arguments == null && argsList.Count > 0) throw new CommandLineArgumentException("Unexpected argument on command line");
            foreach (var o in argsList) _arguments.Add(command, o);
            argsList.Clear();
        }

        public ArgumentsAttribute Arguments => _arguments?.Attribute;

        public IEnumerable<OptionAttribute> Options => _options.OptionsParsed;

        public CommandLineParserOptions _parserOptions = CommandLineParserOptions.NONE;
        private OptionGroupParser _options;
        private Arguments _arguments;
        private MethodInfo _argumentsVerification;
    }
}
