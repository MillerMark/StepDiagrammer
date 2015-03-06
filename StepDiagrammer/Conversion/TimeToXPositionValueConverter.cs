using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace StepDiagrammer
{
  public class TimeToXPositionValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      DateTime dateTime = (DateTime)value;
      TimeSpan span = Session.StepDiagram.GetOffset(dateTime);
      return ConversionHelper.MillisecondsToPixels(span.TotalMilliseconds);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }
  }
}
