using System;

namespace StepDiagrammer
{
  public class TimeMousePoint : TimePoint
  {
    /// <summary>
    /// Initializes a new instance of the TimeMousePoint class.
    /// </summary>
    public TimeMousePoint(int x, int y, bool mouseIsDown)
      : base(x, y)
    {
      MouseIsDown = mouseIsDown;
    }

    public bool MouseIsDown { get; private set; }
  }
}
