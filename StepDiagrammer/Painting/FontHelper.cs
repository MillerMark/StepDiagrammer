using System;
using System.Windows;
using System.Windows.Media;
using System.Globalization;

namespace StepDiagrammer
{
  public static class FontHelper
  {
    public static Size MeasureString(string text, Typeface typeFace, double fontSize)
    {
      string textToMeasure;
      if (text == null || text == string.Empty)
        textToMeasure = " ";
      else
        textToMeasure = text;
      FormattedText formattedText = new FormattedText(textToMeasure, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                                            typeFace, fontSize, Brushes.Black);

      return new Size(formattedText.Width, formattedText.Height);
    }
  }
}
