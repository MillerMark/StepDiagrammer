using System;
using System.Windows.Media;

namespace StepDiagrammer
{
  public abstract class MouseEvent : Event
  {
    protected Color mouseDownAnnotationColor = Color.FromArgb(0x7E, 0xFF, 0x00, 0x00);
    protected Color mouseUpAnnotationColor = Color.FromArgb(0x7E, 0x12, 0x00, 0xFF);

    public static double GetBaseComplexityScore(Event previousEvent)
    {
      double baseScore = 0;
      KeyDownEvent keyDownEvent = previousEvent as KeyDownEvent;
      if (keyDownEvent != null && ComplexityCalculator.LastMouseHandedAction != ActionType.Mouse)
        baseScore = ComplexityCalculator.GetReachingForMouseTransitionCost();  // Transitions between mouse and keyboard are expensive!
      ComplexityCalculator.LastMouseHandedAction = ActionType.Mouse;
      return baseScore;
    }

    static double GetHue()
    {
      return 169;
    }

    protected virtual double GetSaturation()
    {
      return 0.3;
    }

    /// <summary>
    /// Gets the hue and saturation for this event.
    /// </summary>
    /// <param name="hue">A 0-255 value for the hue.</param>
    /// <param name="saturation">A 0-1.0 value for the saturation.</param>
    protected override void GetHueAndSaturation(out double hue, out double saturation)
    {
      hue = GetHue();
      saturation = GetSaturation();
    }
  }
}
