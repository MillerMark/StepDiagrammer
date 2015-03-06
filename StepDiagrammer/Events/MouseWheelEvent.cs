using System;

namespace StepDiagrammer
{
  public class MouseWheelEvent : MouseEvent
  {
    public override double GetMillerComplexityScore(Event previousEvent)
    {
      return GetBaseComplexityScore(previousEvent) + 1;   // Wheel motion is one directional (as opposed to buttons which have a press and a release), so only one point.
    }

    protected override string GetDisplayText()
    {
      return "Mouse Wheel";
    }

    protected override double GetSaturation()
    {
      return 0.3;
    }

    protected override double GetForce()
    {
      return 0.2;
    }
  }
}
