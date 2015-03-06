using System;

namespace StepDiagrammer
{
  [Flags]
  public enum Sides
  {
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8
  }
}
