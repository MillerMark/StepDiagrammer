using System;
using System.Linq;
using System.Collections.Generic;

namespace StepDiagrammer
{
  public class ConversionHelper
  {
    const double DBL_PixelsPerMillisecond = 0.2;  // 1px = 5ms at 100% zoom
    public static void SetAvailableHeight(double height)
    {
      availableHeight = height;
    }

    /// <summary>
    /// Resizes the specified width and height parameters (if needed), maintaining their current aspect ratio, so that 
    /// their area does not exceed the specified maxArea.
    /// </summary>
    public static void ResizeToArea(ref double width, ref double height, int maxArea)
    {
      if (width * height <= maxArea)
        return;

      double ratio = width / height;
      width = Math.Sqrt(maxArea * ratio);
      height = width / ratio;
    }

    static double availableHeight;
    public static double AvailableHeight
    {
      get
      {
        return availableHeight;
      }
    }
    private static double zoomLevel = 1.0;
    public static double ZoomLevel
    {
      get
      {
        return zoomLevel;
      }
      set
      {
        if (zoomLevel == value)
          return;
        zoomLevel = value;
        OnZoomLevelChanged();
      }
    }

    public static double ForceToPixels(double force)
    {
      const double DBL_PercentHeightUsed = 0.9;
      double maxForce;
      if (Session.StepDiagram == null)
        maxForce = 1.0;
      else
        maxForce = Session.StepDiagram.MaxForce;
      return DBL_PercentHeightUsed * availableHeight * force / maxForce;
    }

    public static double ForceToTop(double force)
    {
      return availableHeight - ForceToPixels(force);
    }

    public static double MillisecondsToPixels(double totalMilliseconds)
    {
      return totalMilliseconds * DBL_PixelsPerMillisecond * ZoomLevel;
    }

    public static double PixelsToMilliseconds(double pixels)
    {
      return pixels / (DBL_PixelsPerMillisecond * ZoomLevel);
    }

    public static double GetSpanInPixels()
    {
      Event lastEvent = Session.GetLastEvent();
      if (lastEvent != null)
        return MillisecondsToPixels(Session.StepDiagram.GetOffset(lastEvent.Stop).TotalMilliseconds);
      return 0;
    }

    public static void OnZoomLevelChanged()
    {
      EventHandler handler = ZoomLevelChanged;
      if (handler != null)
        handler(null, EventArgs.Empty);
    }

    public static event EventHandler ZoomLevelChanged;
  }
}
