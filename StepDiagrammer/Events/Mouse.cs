using System;

namespace StepDiagrammer
{
  public class Mouse
  {
    public Mouse(double width, double height)
    {
      Width = width;
      Height = height;      
    }
    public double Height { get; private set; }
    public double Width { get; private set; }
  }
}
