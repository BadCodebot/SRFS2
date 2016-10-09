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

    public class ParameterParser : IOptionParser {

        public ParameterParser(PropertyInfo info, ParameterAttribute attribute) {
            _property = info;
            _attribute = attribute;

            if (!_attribute.HasShortForm && !_attribute.HasLongForm) throw new NotSupportedException("Attribute must have a short form and/or a long form.");
            if (_attribute.IsRequired && _attribute.OptionalArgument != null)
                throw new NotSupportedException("Required parameters cannot have optional arguments.");

            if (_attribute.OptionalArgument != null) {
                PropertyInfo optional = (from p in _property.DeclaringType.GetProperties() where p.Name == _attribute.OptionalArgument select p).SingleOrDefault();
                if (optional == null) throw new NotSupportedException("Could not find optional argument property.");
                if (optional.PropertyType != typeof(bool)) throw new NotSupportedException("Optional argument property is not a bool.");
            }
        }

        #region Properties

        public bool Used { get; private set; } = false;

        #endregion
        #region Methods

        public bool MatchLongForm(object command, string optionName, string optionValue, LinkedList<string> args) {
            if (_attribute.LongForm != optionName) return false;
            processMatch(command, optionValue, args);
            return true;
        }

        public bool MatchShortForm(object command, char optionName, string optionValue, LinkedList<string> args) {
            if (_attribute.ShortForm != optionName) return false;
            processMatch(command, optionValue, args);
            return true;
        }

        private bool processMatch(object command, string optionValue, LinkedList<string> args) {
            Func<string, object> converter = null;
            if (_attribute.Converter != null) {
                converter = x => _property.DeclaringType.GetMethod(_attribute.Converter, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Invoke(command, new object[] { x });
            } else {
                converter = x => TypeDescriptor.GetProperties(_property.DeclaringType).Cast<PropertyDescriptor>().Single(p => p.Name == _property.Name).Converter.
                    ConvertFromString(x);
            }

            Used = true;
            if (_attribute.OptionalArgument != null) {
                command.GetType().GetProperty(_attribute.OptionalArgument).SetValue(command, true);
                if (optionValue != null) _property.SetValue(command, converter(optionValue));
            } else {
                if (optionValue == null) {
                    // The parameter value must be in the next operand
                    if (args.Count == 0)
                        throw new CommandLineArgumentException($"Parameter missing for --{_attribute.LongForm}.");
                    _property.SetValue(command, converter(args.First.Value));
                    args.RemoveFirst();
                } else {
                    _property.SetValue(command, converter(optionValue));
                }
            }
            return true;
        }

        public void Verify(object command) {

            if (!Used && _attribute.EnvironmentalVariable != null) {
                string v = Environment.GetEnvironmentVariable(_attribute.EnvironmentalVariable);
                if (v != null) {
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(_property.DeclaringType).Cast<PropertyDescriptor>().Single(p => p.Name == _property.Name);
                    _property.SetValue(command, descriptor.Converter.ConvertFromString(v));
                    Used = true;
                }
            }
            if (!Used && _attribute.IsRequired) {
                throw new CommandLineArgumentException(
                    $"Required { (_attribute.HasShortForm ? $"-{_attribute.ShortForm}" : $"--{_attribute.LongForm}") } option missing" +
                    $"{(_attribute.EnvironmentalVariable != null ? $" and '${_attribute.EnvironmentalVariable} not set" : "")}.");
            }
        }

        #endregion

        public IEnumerable<OptionAttribute> OptionsParsed { get { yield return _attribute; } }

        private ParameterAttribute _attribute;
        private PropertyInfo _property;
    }

}
