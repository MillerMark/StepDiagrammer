using System;
using System.Threading;
using System.Diagnostics;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace StepDiagrammer
{
  class CpuUsageCalculator
  {
    const int INT_MinTimeBetweenChecks = 250;

    TimeSpan previousTotalProcessorTime;
    FileTime previousKernelTime;
    FileTime previousUserTime;
    DateTime lastCheckTime;
    int lastProcessId;
    long checkCounter;
    Int16 cpuUsage;

    public CpuUsageCalculator()
    {
      previousTotalProcessorTime = TimeSpan.MinValue;
      previousKernelTime.dwHighDateTime = 0;
      previousKernelTime.dwLowDateTime = 0;
      previousUserTime.dwHighDateTime = 0;
      previousUserTime.dwLowDateTime = 0;
      lastCheckTime = DateTime.MinValue;
      lastProcessId = int.MinValue;
      checkCounter = 0;
      cpuUsage = -1;
    }

    public short GetUsage(Process process)
    {
      short savedCpuUsage = cpuUsage;
      if (Interlocked.Increment(ref checkCounter) == 1)
      {
        TimeSpan timeSpanSinceLastCheck = DateTime.Now - lastCheckTime;
        if (timeSpanSinceLastCheck.TotalMilliseconds <= INT_MinTimeBetweenChecks)
        {
          Interlocked.Decrement(ref checkCounter);
          return savedCpuUsage;
        }

        FileTime idleTime, kernelTime, userTime;

        TimeSpan totalProcessorTime = process.TotalProcessorTime;

        if (!Win.GetSystemTimes(out idleTime, out kernelTime, out userTime))
        {
          Interlocked.Decrement(ref checkCounter);
          return savedCpuUsage;
        }

        if (lastProcessId == process.Id && lastCheckTime != DateTime.MinValue)
        {
          UInt64 userTicks = GetTickDelta(userTime, previousUserTime);
          UInt64 kernelTicks = GetTickDelta(kernelTime, previousKernelTime);

          UInt64 totalSystemTicks = kernelTicks + userTicks;
          Int64 processTotalTicks = totalProcessorTime.Ticks - previousTotalProcessorTime.Ticks;

          if (totalSystemTicks > 0)
            cpuUsage = (short)((100.0 * processTotalTicks) / totalSystemTicks);
        }
        else
          cpuUsage = -1;

        lastProcessId = process.Id;

        previousTotalProcessorTime = totalProcessorTime;
        previousKernelTime = kernelTime;
        previousUserTime = userTime;

        lastCheckTime = DateTime.Now;

        savedCpuUsage = cpuUsage;
      }
      Interlocked.Decrement(ref checkCounter);

      if (lastProcessId != process.Id)
        return -1;

      return savedCpuUsage;
    }

    static UInt64 GetTimeInTicks(FileTime a)
    {
      return ((UInt64)(a.dwHighDateTime << 32)) | (UInt64)a.dwLowDateTime;
    }

    private UInt64 GetTickDelta(FileTime a, FileTime b)
    {
      return GetTimeInTicks(a) - GetTimeInTicks(b);
    }
  }
}
