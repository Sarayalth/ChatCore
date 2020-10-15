using System;

namespace ChatCore.Utilities
{
	public static class TimeUtils
	{
		public static string ToShortString(this TimeSpan span)
		{
			return $"{Math.Floor(span.TotalHours):00}:{span.Minutes:00}:{span.Seconds:00}";
		}
	}
}