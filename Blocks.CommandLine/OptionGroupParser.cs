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

    internal class OptionGroupParser : IOptionParser {

        public OptionGroupParser(Type optionGroupClassType) : this(optionGroupClassType, o => o) { }

        public OptionGroupParser(PropertyInfo property) : this(property.PropertyType, o => property.GetValue(o)) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionGroupClassType">The type of the property that the OptionGroupAttribute was tied to.  
        /// We will search through this type for more OptionAttributes and OptionGroupAttributes to fill out all the possible options.</param>
        /// <param name="getValue"></param>
        private OptionGroupParser(Type optionGroupClassType, Func<object, object> getValue) {
            _getValue = getValue;

            // Let's find all the applicable Attributes (they implement IOptionParser).  We must tie each one to a specific property on a class type 
            // (Attributes don't naturally know to what they are applied) so that we can properly set the value for that property.  This is done in 
            // an OptionParser class which also provides provides methods to identify when the option is selected and then update the corresponding object.
            _options.AddRange(from p in optionGroupClassType.GetProperties()
                              from a in p.GetCustomAttributes() where a is IOptionParserFactory
                              select ((IOptionParserFactory)a).CreateParser(p));

            // The user can override the default Verify method, which just loops through all the options and calls verify on them, by marking a method
            // with the Verify attribute.  This is done via attributes so that the user does not need to derive from any of these classes, but can define
            // their own heirarchy and just glue these attributes on.  It's slower, of course, but command line option parsing shouldn't really take much
            // time in the scheme of things.
            _verifyMethod = (from m in optionGroupClassType.GetMethods()
                             from a in m.GetCustomAttributes<OptionVerificationAttribute>()
                             select m).SingleOrDefault();
            if (_verifyMethod != null) {
                // Check the return and parameter types
                if (_verifyMethod.ReturnType != typeof(void)) throw new NotSupportedException("Verify method does not return void.");
                if (_verifyMethod.GetParameters().Length != 0) throw new NotSupportedException("Verify method does not take parameters.");
            }
        }

        public bool MatchLongForm(object optionGroupParent, string optionName, string optionValue, LinkedList<string> args) {
            // Just loop through all the IOptionParsers, stopping when we find one that works.
            foreach (var o in _options) if (o.MatchLongForm(_getValue(optionGroupParent), optionName, optionValue, args)) return true;
            return false;
        }

        public bool MatchShortForm(object optionGroupParent, char optionName, string optionValue, LinkedList<string> args) {
            // Just loop through all the IOptionParsers, stopping when we find one that works.
            foreach (var o in _options) if (o.MatchShortForm(_getValue(optionGroupParent), optionName, optionValue, args)) return true;
            return false;
        }

        public void Verify(object optionGroupParent) {
            // If a verify method was specified, call it.
            if (_verifyMethod != null) _verifyMethod.Invoke(optionGroupParent, new object[0]);
            // Otherwise the default behavior is to loop through each option parser and call their verify method.
            else foreach (var o in _options) o.Verify(_getValue(optionGroupParent));
        }

        public IEnumerable<OptionAttribute> OptionsParsed => from x in _options from y in x.OptionsParsed select y;

        // Specifies how to get the instance of this specific option group (the one which holds the properties, etc, we're assigning) from the parent (i.e.
        // the instance that contains this specific option group).  
        private readonly Func<object, object> _getValue;

        private readonly MethodInfo _verifyMethod;

        private List<IOptionParser> _options = new List<IOptionParser>();
    }
}
