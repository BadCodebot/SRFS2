using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using SRFS;


namespace TrackUtility {

    public class SizeConverter : IValueConverter {

        private const Decimal OneKiloByte = 1024M;
        private const Decimal OneMegaByte = OneKiloByte * 1024M;
        private const Decimal OneGigaByte = OneMegaByte * 1024M;
        private const Decimal OneTeraByte = OneGigaByte * 1024M;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Decimal size;

            try {
                size = System.Convert.ToDecimal(value);
            } catch (InvalidCastException) {
                throw new NotSupportedException();
            }

            string suffix;
            if (size >= OneTeraByte) {
                size /= OneTeraByte;
                suffix = "TB";
            } else if (size >= OneGigaByte) {
                size /= OneGigaByte;
                suffix = "GB";
            } else if (size >= OneMegaByte) {
                size /= OneMegaByte;
                suffix = "MB";
            } else if (size >= OneKiloByte) {
                size /= OneKiloByte;
                suffix = "kB";
            } else {
                suffix = " B";
            }

            return $"{size:N2}{suffix}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
