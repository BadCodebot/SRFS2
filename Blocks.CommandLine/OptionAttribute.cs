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

    public interface IOptionParserFactory {
        IOptionParser CreateParser(PropertyInfo property);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public abstract class OptionAttribute : Attribute, IOptionParserFactory {

        public string Description {
            get {
                return _description;
            }
            set {
                _description = value == string.Empty ? null : value;
            }
        }
        private string _description;

        public char ShortForm {
            get {
                return _selector;
            }
            set {
                if (char.IsLetterOrDigit(value) || char.IsPunctuation(value) || value == '\0') _selector = value;
                else throw new NotSupportedException("Option short form must be a letter, digit, punctuation or the null character.");
            }
        }
        private char _selector = '\0';
        public bool HasShortForm => _selector != '\0';

        public abstract IOptionParser CreateParser(PropertyInfo property);

        public string LongForm { get; set; } = null;
        public bool HasLongForm => LongForm != null;
    }
}
