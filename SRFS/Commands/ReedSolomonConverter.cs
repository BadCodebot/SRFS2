using System;
using System.ComponentModel;
using System.Globalization;

namespace SRFS.Commands {

    public class ReedSolomonConverter : TypeConverter {

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                string[] v = ((string)value).Split(',');
                int n;
                int k;

                if (v.Length != 2 || !int.TryParse(v[0], out n) || !int.TryParse(v[1], out k))
                    throw new FormatException("Invalid Reed-Solomon format.");

                return new ReedSolomon(n, k);
            } else {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
