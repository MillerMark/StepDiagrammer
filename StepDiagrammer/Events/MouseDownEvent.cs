using System;
using System.Windows;
using System.Windows.Media;

namespace StepDiagrammer
{
  public class MouseDownEvent : MouseEvent, IScreenShotAnnotator
  {
    private Point position;

    double GetPreviousMouseTravelDistance(Event previousEvent)
    {
      MouseMoveEvent mouseMoveEvent = previousEvent as MouseMoveEvent;
      if (mouseMoveEvent == null || mouseMoveEvent.Stop < Start - TimeSpan.FromMilliseconds(300))
        return 0;

      System.Collections.ObjectModel.ObservableCollection<Event> events = Session.StepDiagram.Events;
      int index = events.IndexOf(previousEvent);
      while (index > 0 && events[index - 1] is MouseMoveEvent && previousEvent.Start - events[index - 1].Stop < TimeSpan.FromMilliseconds(260))
      {
        previousEvent = events[index - 1];
        index--;
      }
      double straightDiagonalDistance = MouseMoveEvent.GetDistanceBetweenPoints(mouseMoveEvent.StartPosition, this.Position);
      return straightDiagonalDistance;
    }

    public override double GetMillerComplexityScore(Event previousEvent)
    {
      return GetBaseComplexityScore(previousEvent) + MillerComplexity.GetMouseDownScore(Duration.TotalMilliseconds) + 
        MillerComplexity.GetTargetPressScore(GetPreviousMouseTravelDistance(previousEvent), MillerComplexity.DefaultMouseTargetWidth);
    }

    protected override string GetShortDisplayText()
    {
      if (IsClick)
        return "Click";
      else
        return "Mouse Down";
    }

    protected override string GetDisplayText()
    {
      string positionStr = String.Format(" ({0}, {1})", position.X, position.Y);
      if (IsClick)
        return "Click" + positionStr;
      else
        return "Mouse Down" + positionStr;
    }
    
    protected override double GetForce()
    {
      if (Duration.TotalMilliseconds > 600)
        return 1.0;   // More force tends to be required to keep the button held down.
      else
        return 0.82;    // Newtons of force to get a click.
    }

    public void SetPosition(int x, int y)
    {
      position = new Point(x, y);
      ScreenCapture.SetAreaOfFocus(ScreenCapture.GetScreenBoundsAt(x, y));
      CaptureScreen();
    }

    protected override double GetSaturation()
    {
      return 1.0;
    }

    public bool IsClick { get; set; }

    public void AnnotateScreenshot(DrawingContext context, ScreenGrab screenGrab)
    {
      Point clientPt = screenGrab.ToClientPoint(position);

      Color mouseAnnotationColor1 = Color.FromArgb(0xA0, 0xFF, 0x00, 0x00);
      SolidColorBrush annotationBrush = CachedBrushes.Get(mouseAnnotationColor1);
      context.DrawEllipse(annotationBrush, null, clientPt, 2.5, 2.5);
      context.DrawEllipse(Brushes.Black, null, clientPt, 0.7, 0.7);

      Color mouseAnnotationColor2 = Color.FromArgb(0x50, 0xFF, 0x00, 0x00);
      SolidColorBrush annotationBrush2 = CachedBrushes.Get(mouseAnnotationColor2);
      context.DrawEllipse(null, new Pen(annotationBrush2, 2), clientPt, 9, 9);

      Color mouseAnnotationColor3 = Color.FromArgb(0x30, 0xFF, 0x00, 0x00);
      SolidColorBrush annotationBrush3 = CachedBrushes.Get(mouseAnnotationColor3);
      context.DrawEllipse(null, new Pen(annotationBrush3, 1), clientPt, 16, 16);
    }

    public override void PostProcess()
    {
      Rect mouseClickRect = new Rect(position.X, position.Y, 1, 1);
      xAdjust = 100;
      yAdjust = 100;
      mouseClickRect.Inflate(xAdjust, yAdjust);    // Crop to a 200x200px square around the click for context.
      ScreenGrabs.Crop(mouseClickRect, ref xAdjust, ref yAdjust);
      base.PostProcess();
    }

    protected override void PrepareToGetPolygonPoints(out double leftIndent, out double baseStretch)
    {
      base.PrepareToGetPolygonPoints(out leftIndent, out baseStretch);
      baseStretch = 1;
    }

    protected override PointCollection GetPolygonPoints(double width, double height, double leftIndent, double rightIndent, double baseStretch)
    {
      return base.GetPolygonPoints(width, height, leftIndent, rightIndent, baseStretch);
    }

    public Point Position
    {
      get
      {
        return position;
      }
    }
  }
}
