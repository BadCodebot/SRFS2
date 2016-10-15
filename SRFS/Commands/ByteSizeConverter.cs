using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SRFS.Commands {

    public class ByteSizeConverter : TypeConverter {

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                string v = (string)value;

                Regex r = new Regex("^([0-9]+)([KMGT]B)?$");
                Match m = r.Match(v.ToUpper());
                if (!m.Success) throw new FormatException();
                long size = long.Parse(m.Groups[1].Value);
                if (m.Groups[2].Length > 0) {
                    switch (m.Groups[2].Value[0]) {
                        case 'K':
                            size *= 1024;
                            break;
                        case 'M':
                            size *= 1024 * 1024;
                            break;
                        case 'G':
                            size *= 1024 * 1024 * 1024;
                            break;
                        case 'T':
                            size *= 1024L * 1024 * 1024 * 1024;
                            break;
                    }
                }
                return size;
            } else {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
