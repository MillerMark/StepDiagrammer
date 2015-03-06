using System;


namespace StepDiagrammer
{
  public static class DateTimeExtensions
  {
    public static string AsDisplayStr(this TimeSpan span)
    {
      string result = string.Empty;
      if (span.Days > 0)
        if (span.Days > 1)
          result += span.Days + " days, ";
        else
          result += "1 day, ";
      if (span.Hours > 0 || result != string.Empty)
        if (span.Hours > 1)
          result += span.Hours + " hours, ";
        else
          result += "1 hour, ";
      if (span.Minutes > 0 || result != string.Empty)
        result += String.Format("{0}:", span.Minutes);
      int milliseconds = span.Milliseconds;
      int seconds = span.Seconds;
      string negativeStr = string.Empty;
      if (milliseconds < 0 || seconds < 0)
        negativeStr = "-";
      result += String.Format(negativeStr + "{0}.{1:000.}s", Math.Abs(seconds), Math.Abs(milliseconds));
      return result;
    }

    public static bool IsToday(this DateTime dateTime)
    {
      DateTime today = DateTime.Now;
      return dateTime.IsSameDay(today);
    }

    public static bool IsSameDay(this DateTime dateTime, DateTime today)
    {
      return dateTime.Day == today.Day && dateTime.Month == today.Month && dateTime.Year == today.Year;
    }
  }
}
