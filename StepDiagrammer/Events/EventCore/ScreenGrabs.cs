using System;
using System.Windows;
using System.Collections.Generic;

namespace StepDiagrammer
{
  public class ScreenGrabs
  {
    List<ScreenGrab> allGrabs = new List<ScreenGrab>();

    public void Clear()
    {
      foreach (ScreenGrab grab in allGrabs)
        grab.Clear();
      allGrabs.Clear();
    }
    /// <summary>
    /// Crops all screen grabs to the specified rect. If the specified rect extends beyond the screenshot's top or left sides,
    /// xAdjust and/or yAdjust will be non-zero.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="xAdjust"></param>
    /// <param name="yAdjust"></param>
    public void Crop(Rect rect, ref double xAdjust, ref double yAdjust)
    {
      foreach (ScreenGrab grab in allGrabs)
      {
        Rect adjustedRect = rect;
        adjustedRect.Offset(-grab.TopLeft.X, -grab.TopLeft.Y);
        grab.Crop(adjustedRect, ref xAdjust, ref yAdjust);
      }
    }

    public void Add(ScreenGrab grab)
    {
      allGrabs.Add(grab);
    }

    public List<ScreenGrab> AllGrabs
    {
      get
      {
        return allGrabs;
      }
    }
  }
}
