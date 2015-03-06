using System;

namespace StepDiagrammer
{
  public class TaskActive : TimeEntry
  {
    public bool NeedsScreenGrab()
    {
      return ScreenGrabs.AllGrabs.Count == 0;
    }

    public void CaptureActiveWindow()
    {
      CaptureActiveWindow(Handle);
    }
    public void CaptureActiveWindow(IntPtr handle)
    {
      Win.RECT rect;
      if (Win.GetWindowRect(handle, out rect))
        ScreenGrabs.Add(ScreenCapture.Grab(rect));
    }

    public TaskActive(string windowName, IntPtr handle)
    {
      Handle = handle;
      WindowName = windowName;
      Start = DateTime.Now;
      Stop = Start;
      maxPixelsInScreenGrabPreview = 500*500;
    }

    protected override string GetDisplayText()
    {
      return WindowName;
    }

    public string WindowName { get; set; }
    public IntPtr Handle { get; private set; }
  }
}
