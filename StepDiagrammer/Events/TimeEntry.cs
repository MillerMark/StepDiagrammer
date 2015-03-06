using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace StepDiagrammer
{
  public abstract class TimeEntry : INotifyPropertyChanged
  {
    protected int maxPixelsInScreenGrabPreview = int.MinValue;
    ScreenGrabs screenGrabs = new ScreenGrabs();
    public DateTime Start { get; set; }
    public DateTime Stop { get; set; }

    public TimeEntry()
    {
      Start = DateTime.Now;
      Stop = DateTime.Now;
    }

    public BitmapSource InitialScreenGrab
    {
      get
      {
        if (HookEngine.Listening)
          return null;
        if (screenGrabs.AllGrabs.Count > 0)
          return screenGrabs.AllGrabs[0].Image;
        else
          return null;
      }
    }

    public Visibility ScreenGrabVisibility
    {
      get
      {
        if (InitialScreenGrab != null)
          return Visibility.Visible;
        return Visibility.Hidden;
      }
    }

    int GetScreenGrabWidth(BitmapSource initialScreenGrab)
    {
      if (maxPixelsInScreenGrabPreview == int.MinValue)
        return initialScreenGrab.PixelWidth;
      else
      {
        double width = initialScreenGrab.PixelWidth;
        double height = initialScreenGrab.PixelHeight;
        ConversionHelper.ResizeToArea(ref width, ref height, maxPixelsInScreenGrabPreview);
        return (int)Math.Round(width);
      }
    }

    public int ScreenGrabWidth
    {
      get
      {
        BitmapSource initialScreenGrab = InitialScreenGrab;
        if (initialScreenGrab != null)
          return GetScreenGrabWidth(initialScreenGrab);
        return 0;
      }
    }

    int GetScreenGrabHeight(BitmapSource initialScreenGrab)
    {
      if (maxPixelsInScreenGrabPreview == int.MinValue)
        return initialScreenGrab.PixelHeight;
      else
      {
        double width = initialScreenGrab.PixelWidth;
        double height = initialScreenGrab.PixelHeight;
        ConversionHelper.ResizeToArea(ref width, ref height, maxPixelsInScreenGrabPreview);
        return (int)Math.Round(height);
      }
    }

    public int ScreenGrabHeight
    {
      get
      {
        BitmapSource initialScreenGrab = InitialScreenGrab;
        if (initialScreenGrab != null)
          return GetScreenGrabHeight(initialScreenGrab);
        return 0;
      }
    }

    public void CaptureScreen()
    {
      ScreenGrab screenGrab = ScreenCapture.GrabAreaOfFocus();
      if (screenGrab != null)
        screenGrabs.Add(screenGrab);
    }

    void AnnotateScreenshot(IScreenShotAnnotator screenShotAnnotator, ScreenGrab screenGrab)
    {
      DrawingVisual visual = new DrawingVisual();
      double screenGrabWidth = screenGrab.Image.Width;
      double screenGrabHeight = screenGrab.Image.Height;
      using (DrawingContext drawingContext = visual.RenderOpen())
      {
        drawingContext.DrawImage(screenGrab.Image, new Rect(0, 0, screenGrabWidth, screenGrabHeight));
        screenShotAnnotator.AnnotateScreenshot(drawingContext, screenGrab);
      }

      RenderTargetBitmap newImage = new RenderTargetBitmap((int)screenGrabWidth, (int)screenGrabHeight, 96, 96, PixelFormats.Pbgra32);
      newImage.Render(visual);
      screenGrab.Image = newImage;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();
    }

    public virtual void Clear()
    {
      screenGrabs.Clear();
    }

    /// <summary>
    /// Called after all the data is collected.
    /// </summary>
    public virtual void PostProcess()
    {
      IScreenShotAnnotator screenShotAnnotator = this as IScreenShotAnnotator;
      if (screenShotAnnotator != null)
        if (ScreenGrabs.AllGrabs.Count > 0)
          AnnotateScreenshot(screenShotAnnotator, ScreenGrabs.AllGrabs[0]);
    }

    protected virtual string GetDisplayText()
    {
      return string.Empty;
    }

    protected virtual string GetShortDisplayText()
    {
      return GetDisplayText();
    }

    public string DisplayText
    {
      get
      {
        return GetDisplayText();
      }
    }

    public string ShortDisplayText
    {
      get
      {
        return GetShortDisplayText();
      }
    }

    public string StartDisplayStr
    {
      get
      {
        TimeSpan span = Session.StepDiagram.GetOffset(Start);
        return span.AsDisplayStr();
      }
    }

    public string DurationDisplayStr
    {
      get
      {
        return Duration.AsDisplayStr();
      }
    }

    public TimeSpan Duration
    {
      get
      {
        return Stop - Start;
      }
    }

    public TimeSpan Age
    {
      get
      {
        return DateTime.Now - Stop;
      }
    }

    protected void PropertyHasChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public ScreenGrabs ScreenGrabs
    {
      get
      {
        return screenGrabs;
      }
    }

    public void EndsNow()
    {
      Stop = DateTime.Now;
      PropertyHasChanged("Stop");
    }
  }
}
