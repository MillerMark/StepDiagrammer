using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows;

namespace StepDiagrammer
{
  public static class VectorAssist
  {
    public static PointCollection GetSpikePoints(double width, double height, double leftIndent, double rightIndent, double baseStretch)
    {
      PointCollection points = new PointCollection();

      double cornerIndent = 4;
      if (cornerIndent > width / 5.0)
        cornerIndent = width / 5.0;
      double declineDistanceFromRight = cornerIndent * 2;
      points.Add(new Point(leftIndent, cornerIndent));
      points.Add(new Point(leftIndent + cornerIndent, 0));
      points.Add(new Point(rightIndent - declineDistanceFromRight - cornerIndent, 0));
      points.Add(new Point(rightIndent - declineDistanceFromRight, cornerIndent));
      points.Add(new Point(rightIndent - cornerIndent + baseStretch, height / 2 + cornerIndent / 2));
      points.Add(new Point(width + baseStretch, height));
      points.Add(new Point(0 - baseStretch, height));
      points.Add(new Point(leftIndent, cornerIndent));
      return points;
    }
  }
}
