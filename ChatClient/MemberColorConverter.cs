using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ChatClient
{
    public class MemberColorConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] != null &&
                values[1] != null)
            {
                string memberId = values[0].ToString();
                string currentUserId = values[1].ToString();

                if (memberId == currentUserId)
                {
                    return Brushes.Blue;
                }
            }

            return Brushes.Black;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}