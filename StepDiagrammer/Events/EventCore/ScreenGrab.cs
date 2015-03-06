using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StepDiagrammer
{
  public class ScreenGrab
  {
    public BitmapSource Image { get; set; }
    public DateTime Time { get; set; }
    public Point TopLeft { get; set; }

    public void Clear()
    {
      Image = null;
    }

    public void Crop(Rect rect, ref double xAdjust, ref double yAdjust)
    {
      double x = rect.X;
      double y = rect.Y;
      double width = rect.Width;
      double height = rect.Height;

      if (x < 0)
      {
        xAdjust += x;
        width += x;    // x is negative - this will actually shorten the width.
        x = 0;
      }

      if (y < 0)
      {
        yAdjust += y;
        height += y;    // y is negative - this will actually shorten the height.
        y = 0;
      }

      int imageWidth = Image.PixelWidth;
      if (x + width > imageWidth)
        width = imageWidth - x;

      int imageHeight = Image.PixelHeight;
      if (y + height > imageHeight)
        height = imageHeight - y;

      int rectX = (int)Math.Ceiling(x);
      int rectY = (int)Math.Ceiling(y);
      int rectWidth = (int)Math.Ceiling(width);
      int rectHeight = (int)Math.Ceiling(height);

      Int32Rect int32Rect = new Int32Rect(rectX, rectY, rectWidth, rectHeight);
      Image = new CroppedBitmap(Image, int32Rect);
      TopLeft = new Point(TopLeft.X + rectX, TopLeft.Y + rectY);
    }

    /// <summary>
    /// Converts a screen point to a client point of this image.
    /// </summary>
    public Point ToClientPoint(Point screenPoint)
    {
      return new Point(screenPoint.X - TopLeft.X, screenPoint.Y - TopLeft.Y);
    }
  }
}
