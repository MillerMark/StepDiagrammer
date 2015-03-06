using System;
using System.Windows.Media;

namespace StepDiagrammer
{
  public interface IScreenShotAnnotator
  {
    void AnnotateScreenshot(DrawingContext context, ScreenGrab screenGrab);
  }
}
