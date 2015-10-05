using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EvoPrinterUI
{
    [ValueConversion(typeof(Uri),typeof(string))]
    class uriCOnverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var uri = value as Uri;
            if (null == value)
                return null;

            return String.Format("{0}{1}", uri.DnsSafeHost, uri.Port == 80 ? "" : ":" + uri.Port.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Uri)
                return value;
            else if (value is string)
            {
                var val = value as String;
                if(String.IsNullOrWhiteSpace(val))
                    return null;

                if(!val.ToLowerInvariant().Trim().StartsWith("http://"))
                    val = "http://" + val;

                return new Uri(val);

            }

            return null;
        }
    }
}
