using System;
using System.Collections.Generic;
using System.Linq;

namespace StepDiagrammer
{
  public class MillerComplexity
  {
    /// <summary>
    /// Default mouse target width, in pixels. 
    /// 
    /// Note: Calculating mouse target sizes is less than ideal. In the future it would be nice if the app being 
    /// tested could send a message to the Step Diagrammer revealing the actual size of the mouse target, which 
    /// would result in a more accurate MCI score.
    /// </summary>
    public const double DefaultMouseTargetWidth = 27;

    /// <summary>
    /// The distance the finger travels to hit the same key a second time (cm).
    /// </summary>
    public const double DistanceToRepeatHitKey = 0.4;  // cm
      

    /// <summary>
    /// The minimum cost of a mouse click (mouse down followed by a release). For every ClickSegment 
    /// milliseconds, multiply the total cost by this amount. So a mouse down that lasts for one second 
    /// produces a cost of 1.0.
    /// </summary>
    const double CostOfMouseClick = 0.25;

    /// <summary>
    /// The number of milliseconds in what would be considered a click (for purposes of calculating complexity metrics).
    /// </summary>
    const double ClickSegment = 250.0;  // ms

    /// <summary>
    /// This constant normalizes the index of difficulty (based on Fitts's Law) so that 
    /// clicking a standard size key that is totally surrounded by other keys (no empty 
    /// space on any side), with a finger travel distance of only one key (e.g., the 
    /// finger lifts up and clicks the next key over), produces a cost of 1.0.
    /// </summary>
    const double DifficultySlope = 1 / 0.90646800019733764;

    public static double GetCostOfRepeatHit(double targetApproachWidth)
    {
      return GetTargetPressScore(DistanceToRepeatHitKey, targetApproachWidth);
    }

    public static double GetDistance(System.Windows.Point p1, System.Windows.Point p2)
    {
      return GetHypotenuse(p2.X - p1.X, p2.Y - p1.Y);
    }

    public static double GetHypotenuse(double width, double height)
    {
      return Math.Sqrt(height * height + width * width);
    }

    /// <summary>
    /// Returns the effective target width for Fitts's Law calculations. For simplicity 
    /// (and since the approach angle Theta of the finger is challenging to determine with 
    /// any reasonable accuracy knowing only the previous key hit), we take the average 
    /// between the largest possible approach width (the hypotenuse), and the shortest 
    /// possible approach width (the shortest of either width or height).
    /// </summary>
    public static double GetTargetApproachWidth(double width, double height)
    {
      double hypotenuse = GetHypotenuse(width, height);
      double shortestApproachWidth = Math.Min(height, width);
      return (shortestApproachWidth + hypotenuse) / 2.0;
    }

    /// <summary>
    /// Estimates the complexity cost of pressing a key with the specified targetApproachWidth, 
    /// based on the specified finger travel distance (in cm).
    /// </summary>
    /// <param name="travelDistance">The distance the finger must travel (in cm) to reach the center of 
    /// the key.</param>
    /// <param name="EffectiveTargetApproachWidth">The target approach width (in cm)</param>
    public static double GetTargetPressScore(double travelDistance, double targetApproachWidth)
    {
      double indexOfDifficulty = Math.Log(1 + travelDistance / targetApproachWidth, 2);
      return DifficultySlope * indexOfDifficulty;
    }

    public static double GetMouseDownScore(double totalMilliseconds)
    {
      return CostOfMouseClick * totalMilliseconds / ClickSegment;
    }
  }
}
