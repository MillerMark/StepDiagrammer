using System;

namespace StepDiagrammer
{
  public class MousePad
  {
    public Mouse Mouse { get; set; }
    public int Height { get; private set; }
    public int Width { get; private set; }
    public MousePad(int width, int height)
    {
      Width = width;
      Height = height;      
    }
  }
}
