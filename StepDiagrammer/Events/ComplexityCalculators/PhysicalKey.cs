using System;
using System.Windows;

namespace StepDiagrammer
{
  public class PhysicalKey
  {
    const double DBL_FingerPadRadius = 0.7d; // Average radius of human finger pad, in cm

    public PhysicalKey(string name, Sides exposedEdges, Size size, KeyboardSection parentSection)
    {
      ParentSection = parentSection;
      Size = size;
      ExposedEdges = exposedEdges;
      Name = name;
    }

    /// <summary>
    /// Calculates the effective button height. If this button has exposed top or bottom edges (IOW, 
    /// no adjacent buttons immediately above or below), that effectively increases the height by half 
    /// the diameter of the user's finger pad (DBL_FingerPadRadius) for each exposed edge (top or bottom), 
    /// reducing the likelihood for error (and reducing necessary cognitive load when hitting this key).
    /// </summary>
    public double EffectiveHeight
    {
      get
      {
        double extension = 0d;
        if (ExposedTop)
          extension += DBL_FingerPadRadius;
        if (ExposedBottom)
          extension += DBL_FingerPadRadius;
        return Size.Height + extension;
      }
    }

    /// <summary>
    /// Calculates the effective button width. If this button has exposed left or right edges (IOW, 
    /// no adjacent buttons immediately left or right), that effectively increases the width by half 
    /// the diameter of the user's finger pad (DBL_FingerPadRadius) for each exposed edge (left or 
    /// right), reducing the likelihood for error (and reducing necessary cognitive load when hitting 
    /// this key).
    /// </summary>
    public double EffectiveWidth
    {
      get
      {
        double extension = 0d;
        if (ExposedLeft)
          extension += DBL_FingerPadRadius;
        if (ExposedRight)
          extension += DBL_FingerPadRadius;
        return Size.Width + extension;
      }
    }

    public double EffectiveTargetApproachWidth
    {
      get
      {
        return MillerComplexity.GetTargetApproachWidth(EffectiveWidth, EffectiveHeight);
      }
    }
    
    private bool ExposedBottom
    {
      get
      {
        return (ExposedEdges & Sides.Bottom) == Sides.Bottom;
      }
    }
    private bool ExposedLeft
    {
      get
      {
        return (ExposedEdges & Sides.Left) == Sides.Left;
      }
    }
    private bool ExposedRight
    {
      get
      {
        return (ExposedEdges & Sides.Right) == Sides.Right;
      }
    }
    private bool ExposedTop
    {
      get
      {
        return (ExposedEdges & Sides.Top) == Sides.Top;
      }
    }

    public int ExposedCornerCount
    {
      get
      {
        int exposedCornerCount = 0;
        if (ExposedBottom)
        {
          if (ExposedLeft)
            exposedCornerCount++;
          if (ExposedRight)
            exposedCornerCount++;
        }
        if (ExposedTop)
        {
          if (ExposedLeft)
            exposedCornerCount++;
          if (ExposedRight)
            exposedCornerCount++;
        }
        return exposedCornerCount;
      }
    }

    public int ExposedEdgeCount
    {
      get
      {
        int exposedEdgeCount = 0;
        if (ExposedBottom)
          exposedEdgeCount++;
        if (ExposedLeft)
          exposedEdgeCount++;
        if (ExposedRight)
          exposedEdgeCount++;
        if (ExposedTop)
          exposedEdgeCount++;
        return exposedEdgeCount;
      }
    }

    public string Name { get; private set; }
    public Sides ExposedEdges { get; private set; }
    public Size Size { get; private set; }
    public KeyboardSection ParentSection { get; private set; }
  }
}
