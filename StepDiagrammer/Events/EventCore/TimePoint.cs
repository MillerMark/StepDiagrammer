using System;
using System.Windows;

namespace StepDiagrammer
{
  public class TimePoint
  {
    public Point Point { get; set; }
    public DateTime Time { get; set; }

    /// <summary>
    /// Initializes a new instance of the TimePoint class.
    /// </summary>
    public TimePoint(int x, int y)
    {
      Point = new Point(x, y);
      Time = DateTime.Now;
    }
  }
}
