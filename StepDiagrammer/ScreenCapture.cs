using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;


namespace StepDiagrammer
{
  public static class ScreenCapture
  {
    static Rect areaOfFocus = Rect.Empty;

    public static Rect GetScreenBoundsAt(int x, int y)
    {
      System.Windows.Forms.Screen screenAtPoint = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(x, y));
      if (screenAtPoint != null)
      {
        System.Drawing.Rectangle screenBounds = screenAtPoint.Bounds;
        return new Rect(screenBounds.Left, screenBounds.Top, screenBounds.Width, screenBounds.Height);
      }
      return Rect.Empty;
    }

    public static void SetAreaOfFocus(Rect areaOfFocus)
    {
      ScreenCapture.areaOfFocus = areaOfFocus;
    }

    public static ScreenGrab Grab(Win.RECT rect)
    {
      int left = rect.Left;
      int top = rect.Top;
      int width = rect.Right - rect.Left;
      int height = rect.Bottom - rect.Top;
      if (width == 0 || height == 0)
        return null;
      return Grab(left, top, width, height);
    }

    public static ScreenGrab Grab(Rect rect)
    {
      int left = (int)Math.Ceiling(rect.Left);
      int top = (int)Math.Ceiling(rect.Top);
      int width = (int)Math.Ceiling(rect.Width);
      int height = (int)Math.Ceiling(rect.Height);
      if (width == 0  || height == 0)
        return null;
      return Grab(left, top, width, height);
    }

    /// <summary>
    /// Grabs the specified area of the screen including alpha blended windows on top such as tool tips and menus.
    /// IMPORTANT: You must call dispose on the bitmap returned by this function as soon as you are done with it.
    /// </summary>
    private static System.Drawing.Bitmap BitBltGrab(int left, int top, int width, int height)
    {
      IntPtr hDesktop = Win.GetDesktopWindow();
      IntPtr dcDesktop = Win.GetWindowDC(hDesktop);
      IntPtr dcTarget = Win.CreateCompatibleDC(dcDesktop);
      IntPtr hTargetBitmap = Win.CreateCompatibleBitmap(dcDesktop, width, height);
      IntPtr hOldBitmap = Win.SelectObject(dcTarget, hTargetBitmap);
      bool success = Win.BitBlt(dcTarget, 0, 0, width, height, dcDesktop, left, top, System.Drawing.CopyPixelOperation.SourceCopy | System.Drawing.CopyPixelOperation.CaptureBlt);
      System.Drawing.Bitmap bitmap = System.Drawing.Bitmap.FromHbitmap(hTargetBitmap);
      Win.SelectObject(dcTarget, hOldBitmap);
      Win.DeleteObject(hTargetBitmap);
      Win.DeleteDC(dcTarget);
      Win.ReleaseDC(hDesktop, dcDesktop);
      return bitmap;
    }

    static ScreenGrab Grab(int left, int top, int width, int height)
    {
      using (System.Drawing.Bitmap screenBmp = BitBltGrab(left, top, width, height))
      {
        IntPtr hBitmap = screenBmp.GetHbitmap();
        BitmapSource image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        Win.DeleteObject(hBitmap);
        return new ScreenGrab() { Image = image, Time = DateTime.Now, TopLeft = new Point(left, top) };
      }
    }
    
    public static ScreenGrab GrabAreaOfFocus()
    {
      if (areaOfFocus != Rect.Empty)
        return Grab(areaOfFocus);
      else
        return GrabAll();
    }

    public static Rect AllScreensRect
    {
      get
      {
        System.Windows.Forms.Screen[] allScreens = System.Windows.Forms.Screen.AllScreens;

        int left = allScreens.Min(screen => screen.Bounds.X);
        int right = allScreens.Max(screen => screen.Bounds.X + screen.Bounds.Width);
        
        int top = allScreens.Min(screen => screen.Bounds.Y);
        int bottom = allScreens.Max(screen => screen.Bounds.Y + screen.Bounds.Height);
        
        int width = right - left;
        int height = bottom - top;

        return new Rect(left, top, width, height);
      }
    }

    public static ScreenGrab GrabAll()
    {
      return Grab(AllScreensRect);
    }
  }
}
