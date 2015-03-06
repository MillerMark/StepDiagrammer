using System;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Animation;

namespace StepDiagrammer
{
  /// <summary>
  /// Animates mouse move and mouse click beacons.
  /// </summary>
  public static class MouseMoveAnimator
  {
    static int midPointIndex = -1;
    static MouseDownEvent mouseDownEvent;
    static MouseMoveEvent mouseMoveEvent;
    readonly static object animatingEventLocker = new object();
    static Image mouseImage;
    static Ellipse mouseBeacon;
    static Ellipse mouseLightRingBeacon;
    static ScreenGrab mouseGrab;

    static void GetCanvasCoordinates(UIElement control, out double left, out double top)
    {
      if (control == null)
      {
        left = 0;
        top = 0;
        return;
      }
      left = Canvas.GetLeft(control);
      if (double.IsNaN(left))
        left = 0;
      top = Canvas.GetTop(control);
      if (double.IsNaN(top))
        top = 0;
    }

    static Point GetClientPoint(Point position)
    {
      Point rawPoint = mouseGrab.ToClientPoint(position);
      const double borderThickness = 1.0;
      return new Point(animationAdjustX + rawPoint.X - borderThickness + 1, animationAdjustY + rawPoint.Y - borderThickness + 1);
    }

    static void QueueMouseAppearAnimation(Point startPosition, EventHandler nextAnimation)
    {
      Point start;
      midPointIndex++;
      if (mouseGrab == null)
        return;
      start = GetClientPoint(startPosition);
          
      TimeSpan fadeTimeSpan = TimeSpan.FromMilliseconds(500);
      TimeSpan moveTimeSpan = TimeSpan.FromMilliseconds(50);
      double mouseOpacity;
      if (mouseDownEvent != null)
        mouseOpacity = 1.0;
      else
        mouseOpacity = 0.75;
      DoubleAnimation fadeIn = new DoubleAnimation(0, mouseOpacity, fadeTimeSpan);
      double canvasLeft;
      double canvasTop;
      GetCanvasCoordinates(mouseImage, out canvasLeft, out canvasTop);
      double beaconLeft;
      double beaconTop;
      GetCanvasCoordinates(mouseBeacon, out beaconLeft, out beaconTop);
      DoubleAnimation xMouseMove = new DoubleAnimation(canvasLeft, start.X, moveTimeSpan);
      DoubleAnimation yMouseMove = new DoubleAnimation(canvasTop, start.Y, moveTimeSpan);
      if (nextAnimation != null)
        fadeIn.Completed += nextAnimation;
      mouseImage.BeginAnimation(Image.OpacityProperty, fadeIn);
      mouseImage.BeginAnimation(Canvas.LeftProperty, xMouseMove);
      mouseImage.BeginAnimation(Canvas.TopProperty, yMouseMove);
      if (mouseDownEvent != null)
      {
        DoubleAnimation xBeaconMove = new DoubleAnimation(beaconLeft, start.X, moveTimeSpan);
        DoubleAnimation yBeaconMove = new DoubleAnimation(beaconTop, start.Y, moveTimeSpan);

        mouseBeacon.BeginAnimation(Canvas.LeftProperty, xBeaconMove);
        mouseLightRingBeacon.BeginAnimation(Canvas.LeftProperty, xBeaconMove);
        mouseBeacon.BeginAnimation(Canvas.TopProperty, yBeaconMove);
        mouseLightRingBeacon.BeginAnimation(Canvas.TopProperty, yBeaconMove);
      }
    }

    static bool WeHaveMovement(Point start, Point next)
    {
      return start.X - next.X != 0 || start.Y - next.Y != 0;
    }

    static void GetNextMouseMoveTransition(out Point startPt, out DateTime startTime, out Point nextPt, out DateTime nextTime)
    {
      if (midPointIndex == 0)
      {
        startPt = mouseMoveEvent.StartPosition;
        startTime = mouseMoveEvent.Start;
      }
      else
      {
        TimeMousePoint firstTimePoint = mouseMoveEvent.MidPoints[midPointIndex - 1];
        startPt = firstTimePoint.Point;
        startTime = firstTimePoint.Time;
      }

      TimeMousePoint nextTimePoint = mouseMoveEvent.MidPoints[midPointIndex];
      nextPt = nextTimePoint.Point;
      nextTime = nextTimePoint.Time;

      midPointIndex++;

      startPt = GetClientPoint(startPt);
      nextPt = GetClientPoint(nextPt);
    }

    static void QueueNextMouseMoveAnimation(object s, EventArgs e)
    {
      lock (animatingEventLocker)
      {
        if (mouseMoveEvent == null)
          return;

        if (midPointIndex == -1)
        {
          QueueMouseAppearAnimation(mouseMoveEvent.StartPosition, QueueNextMouseMoveAnimation);
          return;
        }

        if (midPointIndex >= mouseMoveEvent.MidPoints.Count)
        {
          midPointIndex = -1;
          return;   // We are done animating.
        }

        DateTime startTime;
        Point nextPt;
        DateTime nextTime;
        Point startPt;
        GetNextMouseMoveTransition(out startPt, out startTime, out nextPt, out nextTime);

        if (WeHaveMovement(startPt, nextPt))
          AnimateMouseMove(startPt.X, startPt.Y, nextPt.X, nextPt.Y, (nextTime - startTime).TotalMilliseconds);
        else
          QueueNextMouseMoveAnimation(s, e);
      }
    }

    static void QueueNextMouseDownAnimation(object s, EventArgs e)
    {
      lock (animatingEventLocker)
      {
        if (mouseDownEvent == null)
          return;

        AnimateMouseDownBeacon(mouseDownEvent.Position);
      }
    }

    static void AnimateMouseDownBeacon(Point position)
    {
      TimeSpan timeSpan = TimeSpan.FromMilliseconds(800);

      if (mouseGrab == null)
        return;
      Point start = GetClientPoint(position);
      double left = start.X + 1;
      double top = start.Y + 2;
      double radiusGrowth = 16;
      const double startingThickness = 6;
      const double endingThickness = 1;
      DoubleAnimation leftAnimation = new DoubleAnimation(left, left - radiusGrowth, timeSpan);
      DoubleAnimation topAnimation = new DoubleAnimation(top, top - radiusGrowth, timeSpan);
      DoubleAnimation diameterAnimation = new DoubleAnimation(0, 2 * radiusGrowth, timeSpan);
      DoubleAnimation thicknessAnimation = new DoubleAnimation(startingThickness, endingThickness, timeSpan);
      DoubleAnimation opacityAnimation = new DoubleAnimation(1.0, 0.0, timeSpan);

      mouseBeacon.BeginAnimation(Canvas.LeftProperty, leftAnimation);
      mouseBeacon.BeginAnimation(Canvas.TopProperty, topAnimation);
      mouseBeacon.BeginAnimation(Ellipse.WidthProperty, diameterAnimation);
      mouseBeacon.BeginAnimation(Ellipse.HeightProperty, diameterAnimation);
      mouseBeacon.BeginAnimation(Ellipse.StrokeThicknessProperty, thicknessAnimation);
      mouseBeacon.BeginAnimation(Ellipse.OpacityProperty, opacityAnimation);

      DoubleAnimation leftLightRingAnimation = new DoubleAnimation(left - startingThickness / 2, left - radiusGrowth + endingThickness / 2, timeSpan);
      DoubleAnimation topLightRingAnimation = new DoubleAnimation(top - startingThickness / 2, top - radiusGrowth + endingThickness / 2, timeSpan);
      DoubleAnimation diameterLightRingAnimation = new DoubleAnimation(startingThickness, 2 * (radiusGrowth + endingThickness), timeSpan);

      mouseLightRingBeacon.BeginAnimation(Canvas.LeftProperty, leftLightRingAnimation);
      mouseLightRingBeacon.BeginAnimation(Canvas.TopProperty, topLightRingAnimation);
      mouseLightRingBeacon.BeginAnimation(Ellipse.WidthProperty, diameterLightRingAnimation);
      mouseLightRingBeacon.BeginAnimation(Ellipse.HeightProperty, diameterLightRingAnimation);
      mouseLightRingBeacon.BeginAnimation(Ellipse.StrokeThicknessProperty, thicknessAnimation);
      mouseLightRingBeacon.BeginAnimation(Ellipse.OpacityProperty, opacityAnimation);

    }

    static void AnimateMouseMove(double fromX, double fromY, double toX, double toY, double milliseconds)
    {
      if (milliseconds < 5)
        milliseconds = 5;
      TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
      DoubleAnimation animVertical = new DoubleAnimation(fromY, toY, timeSpan);
      DoubleAnimation animHorizontal = new DoubleAnimation(fromX, toX, timeSpan);

      if (fromY != toY)
        animVertical.Completed += QueueNextMouseMoveAnimation;
      else
        animHorizontal.Completed += QueueNextMouseMoveAnimation;

      mouseImage.BeginAnimation(Canvas.LeftProperty, animHorizontal);
      mouseImage.BeginAnimation(Canvas.TopProperty, animVertical);
    }

    static double animationAdjustX;
    static double animationAdjustY;
      
    public static void TooltipLoaded(Grid parentGrid)
    {
      if (parentGrid == null)
        return;

      animationAdjustX = 0;
      animationAdjustY = 0;
      Image screenGrab = VisualTree.FindChild<Image>(parentGrid, "imgScreenGrab");
      if (screenGrab != null)
      {
        Border border = screenGrab.Parent as Border;
        if (border != null)
        {
          double deltaWidth = border.ActualWidth - screenGrab.ActualWidth;
          if (deltaWidth > 4.0)
            animationAdjustX = deltaWidth / 2 - 4.0;

          double deltaHeight = border.ActualHeight - screenGrab.ActualHeight;
          if (deltaHeight > 4.0)
            animationAdjustY = deltaHeight / 2 - 4.0;
        }
      }

      mouseImage = VisualTree.FindChild<Image>(parentGrid, "imgMouse");
      if (mouseImage == null)
        return;

      mouseBeacon = VisualTree.FindChild<Ellipse>(parentGrid, "mouseBeacon");
      if (mouseBeacon == null)
        return;

      mouseLightRingBeacon = VisualTree.FindChild<Ellipse>(parentGrid, "mouseLightRingBeacon");
      if (mouseLightRingBeacon == null)
        return;

      mouseGrab = null;

      mouseImage.Visibility = Visibility.Hidden;
      mouseBeacon.Visibility = Visibility.Hidden;
      mouseLightRingBeacon.Visibility = Visibility.Hidden;

      ToolTip parentToolTip = VisualTree.GetParent<ToolTip>(parentGrid);
      if (parentToolTip == null)
        return;

      lock (animatingEventLocker)
      {
        mouseMoveEvent = parentToolTip.DataContext as MouseMoveEvent;
        mouseDownEvent = parentToolTip.DataContext as MouseDownEvent;

        if ((mouseMoveEvent == null || mouseMoveEvent.MidPoints.Count <= 0) && mouseDownEvent == null)
          return;

        MouseEvent mouseEvent = parentToolTip.DataContext as MouseEvent;
        List<ScreenGrab> allGrabs = mouseEvent.ScreenGrabs.AllGrabs;
        if (allGrabs.Count <= 0)
          return;
        mouseGrab = allGrabs[0];
        mouseImage.Visibility = Visibility.Visible;

        if (mouseDownEvent != null)
        {
          mouseLightRingBeacon.Width = 0;
          mouseLightRingBeacon.Height = 0;
          mouseLightRingBeacon.Opacity = 0;

          mouseBeacon.Width = 0;
          mouseBeacon.Height = 0;
          mouseBeacon.Opacity = 0;
          QueueMouseAppearAnimation(mouseDownEvent.Position, QueueNextMouseDownAnimation);
          mouseBeacon.Visibility = Visibility.Visible;
          mouseLightRingBeacon.Visibility = Visibility.Visible;
          return;
        }

        midPointIndex = -1;
        QueueNextMouseMoveAnimation(null, null);
      }
    }
  }
}
