﻿using System;

namespace Tests
{
  public static class TimeExtensions
  {
    public static TimeSpan Minutes(this int minutes)
    {
      return TimeSpan.FromMinutes(minutes);
    }

    public static TimeSpan Hours(this int hours)
    {
      return TimeSpan.FromHours(hours);
    }

    public static bool Before(this DateTimeOffset? origin, DateTimeOffset? date)
    {
      return origin < date;
    }

    public static bool After(this DateTimeOffset? origin, DateTimeOffset? date)
    {
      return origin > date;
    }

    public static bool Before(this DateTimeOffset origin, DateTimeOffset? date)
    {
      return origin < date;
    }

    public static bool After(this DateTimeOffset origin, DateTimeOffset? date)
    {
      return origin > date;
    }
  }
}