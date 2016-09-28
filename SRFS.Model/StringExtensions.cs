using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SRFS.Model {

    public static class StringExtensions {
        public static string ToFileSize(this ulong l) {
            return String.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }
        public static string ToFileSize(this long l) {
            return String.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }
        public static string ToFileSize(this uint l) {
            return String.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }
        public static string ToFileSize(this int l) {
            return String.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }
        public static bool TryConvertFromString(this TypeConverter c, string text, out object value) {
            try {
                value = c.ConvertFromString(text);
                return true;
            } catch (Exception) {
                value = null;
                return false;
            }
        }

        public static string ToRanges(this IEnumerable<short> values) => values.ToRanges((a, b) => b == a + 1);
        public static string ToRanges(this IEnumerable<int> values) => values.ToRanges((a, b) => b == a + 1);
        public static string ToRanges(this IEnumerable<long> values) => values.ToRanges((a, b) => b == a + 1);
        public static string ToRanges(this IEnumerable<ushort> values) => values.ToRanges((a, b) => b == a + 1);
        public static string ToRanges(this IEnumerable<uint> values) => values.ToRanges((a, b) => b == a + 1);
        public static string ToRanges(this IEnumerable<ulong> values) => values.ToRanges((a, b) => b == a + 1);

        private static string ToRanges<T>(this IEnumerable<T> values, Func<T, T, bool> adjacent) {
            var sb = new StringBuilder();

            T last = default(T);
            bool first = true;
            bool inRange = false;
            foreach (var v in values) {
                if (first) {
                    sb.Append(v);
                    first = false;
                } else {
                    if (inRange) {
                        if (!adjacent(last, v)) {
                            sb.Append($"-{last},{v}");
                            inRange = false;
                        }
                    } else {
                        if (adjacent(last, v)) {
                            inRange = true;
                        } else {
                            sb.Append($",{v}");
                        }
                    }
                }
                last = v;
            }
            if (inRange) sb.Append($"-{last}");
            return sb.ToString();
        }
    }
}
