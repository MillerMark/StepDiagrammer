using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace StepDiagrammer
{
  public static class CachedBrushes
  {
    static Dictionary<Color, SolidColorBrush> cachedBrushes = new Dictionary<Color, SolidColorBrush>();

    public static SolidColorBrush Get(Color color)
    {
      if (HasBrush(color))
        return cachedBrushes[color];

      return CreateNewBrush(color);
    }

    public static bool HasBrush(Color color)
    {
      return cachedBrushes.ContainsKey(color);
    }

    public static SolidColorBrush CreateNewBrush(Color color)
    {
      SolidColorBrush newBrush = new SolidColorBrush(color);
      cachedBrushes.Add(color, newBrush);
      return newBrush;
    }

    public static void ClearAll()
    {
      cachedBrushes.Clear();
    }
  }
}
