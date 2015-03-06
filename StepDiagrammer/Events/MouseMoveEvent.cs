using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace StepDiagrammer
{
  public class MouseMoveEvent : MouseEvent, IScreenShotAnnotator
  {
    const double DBL_MaxMouseAnnotationPenWidth = 3.0;
    const double DBL_MinMouseAnnotationPenWidth = 0.2;
    Point endPosition;
    List<TimeMousePoint> midPoints = new List<TimeMousePoint>();
    bool mouseStartsDown;
    private double totalDistanceTravelled;
    Point startPosition;

    public MouseMoveEvent()
    {

    }

    public override double GetMillerComplexityScore(Event previousEvent)
    {
      double motionScore = Duration.TotalMilliseconds / 500.0;  // Add 0.2 for every 100ms of mouse movement.
      return GetBaseComplexityScore(previousEvent) + motionScore;  
    }

    protected override string GetDisplayText()
    {
      return String.Format("Mouse Move: {0:0.#}px", TotalDistanceTravelled);
    }

    protected override double GetSaturation()
    {
      return 0.5;
    }

    public void SetEndPosition(Point endPosition)
    {
      this.endPosition = endPosition;
    }

    public void SetStartXY(int x, int y, bool mouseIsDown)
    {
      mouseStartsDown = mouseIsDown;
      totalDistanceTravelled = 0;
      startPosition = new Point(x, y);
      endPosition = new Point(x, y);    // Make sure endPosition is initialized to same value in case we never see the end of this mouse move (in which case there will be no midPoints). If we do have midPoints, the last midPoint will match endPosition. 

      ScreenCapture.SetAreaOfFocus(ScreenCapture.GetScreenBoundsAt(x, y));
      CaptureScreen();
    }

    public void SetEndXY(int x, int y, bool mouseIsDown)
    {
      AddMidPoint(x, y, mouseIsDown);
      endPosition = new Point(x, y);
    }

    void AddMidPoint(int x, int y, bool mouseIsDown)
    {
      Point lastPoint;
      if (midPoints.Count > 0)
        lastPoint = midPoints[midPoints.Count - 1].Point;
      else
        lastPoint = StartPosition;

      TimeMousePoint newTimePoint = new TimeMousePoint(x, y, mouseIsDown);
      Point thisPoint = newTimePoint.Point;
      totalDistanceTravelled += GetDistanceBetweenPoints(lastPoint, thisPoint);

      midPoints.Add(newTimePoint);

      PropertyHasChanged("DisplayText");
    }

    protected override double GetForce()
    {
      return 0.4;      // About 0.4 Newtons of force are required to move the mouse.
    }

    public Point EndPosition
    {
      get { return endPosition; }
    }

    public List<TimeMousePoint> MidPoints
    {
      get
      {
        return midPoints;
      }
    }

    public Point StartPosition
    {
      get
      {
        return startPosition;
      }
    }

    /// <summary>
    /// Adjusts left, right, top and bottom based on the specified point.
    /// </summary>
    static void ExtendBounds(Point pt, ref double left, ref double top, ref double right, ref double bottom)
    {
      if (pt.X < left || left == double.MinValue)
        left = pt.X;
      if (pt.X > right || right == double.MinValue)
        right = pt.X;
      if (pt.Y < top || top == double.MinValue)
        top = pt.Y;
      if (pt.Y > bottom || bottom == double.MinValue)
        bottom = pt.Y;
    }

    public Rect GetMouseMoveBounds()
    {
      double left = double.MinValue;
      double top = double.MinValue;
      double right = double.MinValue;
      double bottom = double.MinValue;
      foreach (TimeMousePoint midPoint in midPoints)
        ExtendBounds(midPoint.Point, ref left, ref top, ref right, ref bottom);

      ExtendBounds(StartPosition, ref left, ref top, ref right, ref bottom);
      ExtendBounds(EndPosition, ref left, ref top, ref right, ref bottom);

      double width = right - left;
      double height = bottom - top;

      return new Rect(left, top, width, height);
    }

    static double GetPenThickness(double speed)
    {
      double penThickness = 0.25 / speed;
      if (penThickness > DBL_MaxMouseAnnotationPenWidth)
        penThickness = DBL_MaxMouseAnnotationPenWidth;
      if (penThickness < DBL_MinMouseAnnotationPenWidth)
        penThickness = DBL_MinMouseAnnotationPenWidth;
      return penThickness;
    }

    public static double GetDistanceBetweenPoints(Point thisPoint, Point nextPoint)
    {
      return Math.Sqrt(Math.Pow(thisPoint.X - nextPoint.X, 2) + Math.Pow(thisPoint.Y - nextPoint.Y, 2));
    }

    Point GetCenterPoint(Point start, Point end)
    {
      return new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
    }

    void DrawLine(DrawingContext context, Point start, Point end, double distance, double time, bool mouseIsDown)
    {
      double speed = distance / time;
      double penThickness = GetPenThickness(speed);

      Color annotationColor = GetMouseAnnotationColor(mouseIsDown);
      SolidColorBrush arrowBrush = CachedBrushes.Get(annotationColor);

      if (penThickness > distance)
        context.DrawEllipse(arrowBrush, null, GetCenterPoint(start, end), penThickness / 2, penThickness / 2);
      else
      {
        Pen arrowPen = new Pen(arrowBrush, penThickness);
        context.DrawLine(arrowPen, start, end);
      }
    }

    Color GetMouseAnnotationColor(bool mouseIsDown)
    {
      return mouseIsDown ? mouseDownAnnotationColor : mouseUpAnnotationColor;
    }

    public void AnnotateScreenshot(DrawingContext context, ScreenGrab screenGrab)
    {
      Point start = screenGrab.ToClientPoint(StartPosition);
      Point end = screenGrab.ToClientPoint(EndPosition);

      bool mouseIsDownAtEnd = false;
      if (midPoints.Count > 1)
      {
        Point thisPoint = start;
        DateTime thisTime = Start;
        foreach (TimeMousePoint midPoint in midPoints)
        {
          DateTime nextTime = midPoint.Time;
          TimeSpan timeBetweenPoints = nextTime - thisTime;
          double time = timeBetweenPoints.TotalMilliseconds;
          Point nextPoint = screenGrab.ToClientPoint(midPoint.Point);
          double distance = GetDistanceBetweenPoints(thisPoint, nextPoint);
          if (thisPoint != nextPoint)
            DrawLine(context, thisPoint, nextPoint, distance, time, midPoint.MouseIsDown);

          if (mouseIsDownAtEnd != midPoint.MouseIsDown)
          {
            mouseIsDownAtEnd = midPoint.MouseIsDown;
            double radius;
            double thickness;
            if (mouseIsDownAtEnd)
            {
              radius = 6;
              thickness = 2;
            }
            else
            {
              radius = 6;
              thickness = 1;
            }
            Color annotationColor = GetMouseAnnotationColor(mouseIsDownAtEnd);
            SolidColorBrush mouseDownBrush = CachedBrushes.Get(annotationColor);
            Pen arrowPen = new Pen(mouseDownBrush, thickness);
            context.DrawEllipse(null, arrowPen, nextPoint, radius, radius);
          }

          thisPoint = nextPoint;
          thisTime = nextTime;
        }
      }
      else if (start != end)
      {
        TimeSpan timeBetweenPoints = Duration;
        double time = timeBetweenPoints.TotalMilliseconds;
        double distance = GetDistanceBetweenPoints(startPosition, endPosition);
        DrawLine(context, start, end, distance, time, mouseStartsDown);
        mouseIsDownAtEnd = mouseStartsDown;
      }

      Color lastEllipseColor = GetMouseAnnotationColor(mouseIsDownAtEnd);
      SolidColorBrush arrowBrush = CachedBrushes.Get(lastEllipseColor);
      context.DrawEllipse(arrowBrush, null, end, 4, 4);
    }

    public override void PostProcess()
    {
      Rect mouseMoveBounds = GetMouseMoveBounds();
      xAdjust = 100;
      yAdjust = 100;
      mouseMoveBounds.Inflate(xAdjust, yAdjust);    // Get more than the mouse actually moved for context (useful for very tiny mouse moves).
      ScreenGrabs.Crop(mouseMoveBounds, ref xAdjust, ref yAdjust);
      base.PostProcess();
    }

    public double TotalDistanceTravelled
    {
      get
      {
        return totalDistanceTravelled;
      }
    }

  }
}
