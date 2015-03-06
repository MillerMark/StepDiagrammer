using System;

namespace StepDiagrammer
{
  public class UsageChangedEventArgs : EventArgs
  {
    public UsageChangedEventArgs()
    {
    }

    public UsageChangedEventArgs(short oldUsage, short newUsage)
    {
      SetValues(oldUsage, newUsage);
    }

    private short newUsage;
    private short oldUsage;
    public void SetValues(short oldUsage, short newUsage)
    {
      this.oldUsage = oldUsage;
      this.newUsage = newUsage;
    }

    public short OldUsage
    {
      get
      {
        return oldUsage;
      }
    }

    public short NewUsage
    {
      get
      {
        return newUsage;
      }
    }
  }
}
