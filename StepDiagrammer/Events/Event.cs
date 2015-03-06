using System;
using System.Windows;
using System.Windows.Media;

namespace StepDiagrammer
{
  public abstract class Event :TimeEntry
  {
    /// <summary>
    /// For mapping recorded screen points to screen shots.
    /// </summary>
    protected double xAdjust;
    protected double yAdjust;
    

    public string Data { get; set; }
    public static double LabelFontSize { get; set; }
    public static Typeface LabelTypeface { get; set; }

    /// <summary>
    /// Returns the complexity score for this user event. Higher numbers mean the event is more 
    /// complex. Mouse button clicks and key presses default to 2 (one for the initial press and one 
    /// for the release). Other actions can return different values. For example, a complicated mouse 
    /// move might return a higher number, or hard to press keys might return a higher number, or easy to 
    /// hit keys (such as the space bar or tab) might return a lower number.
    /// </summary>
    /// <param name="previousEvent">The event that preceeds this one in the timeline.</param>
    public abstract double GetMillerComplexityScore(Event previousEvent);

    public virtual double Left
    {
      get
      {
        TimeSpan span = Session.StepDiagram.GetOffset(Start);
        return ConversionHelper.MillisecondsToPixels(span.TotalMilliseconds);
      }
    }

    public virtual double Top
    {
      get
      {
        return ConversionHelper.ForceToTop(Force);
      }
    }

    /// <summary>
    /// Gets the hue and saturation for this event.
    /// </summary>
    /// <param name="hue">A 0-255 value for the hue.</param>
    /// <param name="saturation">A 0-1.0 value for the saturation.</param>
    protected virtual void GetHueAndSaturation(out double hue, out double saturation)
    {
      hue = 0;
      saturation = 0;
    }

    /// <summary>
    /// Gets the force (in Newtons) associated with this event.
    /// </summary>
    /// <returns></returns>
    protected abstract double GetForce();

    public double Force
    {
      get
      {
        return GetForce();
      }
    }

    public virtual double ShortDisplayTextHeight
    {
      get
      {
        double labelHeight = FontHelper.MeasureString(ShortDisplayText, LabelTypeface, LabelFontSize).Width;  // Returning Width since this text is rotated 90 degrees.
        if (Height - labelHeight < -8)
          return Height - labelHeight + 4;
        else
          return -6;
      }
    }

    protected virtual PointCollection GetPolygonPoints(double width, double height, double leftIndent, double rightIndent, double baseStretch)
    {
      PointCollection points = new PointCollection();

      points.Add(new Point(leftIndent, 0));
      points.Add(new Point(rightIndent, 0));
      points.Add(new Point(width + baseStretch, height));
      points.Add(new Point(0 - baseStretch, height));
      points.Add(new Point(leftIndent, 0));

      return points;
    }

    protected virtual PointCollection GetPolygonPoints(double width, double height, double leftIndent, double baseStretch)
    {
      const int INT_MinPixelsForAClick = 3;

      if (width < INT_MinPixelsForAClick)
        width = INT_MinPixelsForAClick;

      double rightIndent = width - leftIndent;

      return GetPolygonPoints(width, height, leftIndent, rightIndent, baseStretch);
    }

    protected virtual void PrepareToGetPolygonPoints(out double indent, out double baseStretch)
    {
      indent = 0;
      baseStretch = 0;
    }

    public virtual PointCollection PolygonPoints
    {
      get
      {
        double width = Width;
        double height = Height;
        double indent;
        double baseStretch;
        PrepareToGetPolygonPoints(out indent, out baseStretch);
        return GetPolygonPoints(width, height, indent, baseStretch);
      }
    }

    HueSatLight GetFillHueSatLight()
    {
      double hue;
      double sat;
      GetHueAndSaturation(out hue, out sat);

      return new HueSatLight() { Hue = hue / 255.0, Saturation = sat, Lightness = 0.7, Alpha = 0.5 };
    }

    protected virtual Color GetFillColor()
    {
      return GetFillHueSatLight().AsRGB;
    }

    protected virtual Color GetStrokeColor()
    {
      HueSatLight fillHueSatLight = GetFillHueSatLight();
      fillHueSatLight.Lightness /= 2;   // Make color darker.
      return fillHueSatLight.AsRGB;
    }

    public double Width
    {
      get
      {
        return ConversionHelper.MillisecondsToPixels(Duration.TotalMilliseconds);
      }
    }

    public virtual double Height
    {
      get
      {
        return ConversionHelper.ForceToPixels(Force);
      }
    }

    public SolidColorBrush StrokeColor
    {
      get
      {
        return CachedBrushes.Get(GetStrokeColor());
      }
    }

    public SolidColorBrush FillColor
    {
      get
      {
        return CachedBrushes.Get(GetFillColor());
      }
    }
    
    protected static IInputDevices complexityCalculator;
    public static void RegisterComplexityCalculator(IInputDevices map)
    {
      Event.complexityCalculator = map;
    }

    public static IInputDevices ComplexityCalculator
    {
      get
      {
        if (complexityCalculator == null)
        {
          complexityCalculator = new MicrosoftNaturalKeyboard();
          MousePad newMousePad = new MousePad(18, 18);
          newMousePad.Mouse = new Mouse(5.6, 10.2);
          complexityCalculator.AttachMousePad(newMousePad, Handedness.Right);
        }
        return complexityCalculator;
      }
    }
  }
}
