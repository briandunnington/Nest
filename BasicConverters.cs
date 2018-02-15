using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nest
{
    public static class BasicConverters
    {
        public static Func<string, object> IntegerConverter = (val) =>
        {
            int i = 0;
            int.TryParse(val, out i);
            return i;
        };

        public static Func<string, object> DateConverter = (val) =>
        {
            DateTime d = DateTime.MinValue;
            DateTime.TryParse(val, out d);
            return d;
        };

        public static Func<string, object> DoubleConverter = (val) =>
        {
            double d = 0;
            double.TryParse(val, out d);
            return d;
        };
    }
}
