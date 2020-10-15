using System;
using System.Text;

namespace ChatCore.Utilities
{
	public static class StringUtils
	{
		public static bool Contains(this string? source, string toCheck, StringComparison comp)
		{
			return source?.IndexOf(toCheck, comp) >= 0;
		}

		public static string Uncamelcase(this string str)
		{
			var sb = new StringBuilder();
			var upperStreak = 0;
			for (var i = 0; i < str.Length; i++)
			{
				if (i < str.Length - 2)
				{
					var isLower = char.IsLower(str[i]);
					if (!isLower)
					{
						upperStreak++;
					}

					var nextIsLower = char.IsLower(str[i + 1]);
					if (isLower && !nextIsLower)
					{
						sb.Append(str[i]);
						sb.Append(" ");
					}
					else if (!isLower && nextIsLower && upperStreak > 1)
					{
						sb.Append(" ");
						sb.Append(str[i]);
					}
					else
					{
						sb.Append(str[i]);
					}

					if (isLower)
					{
						upperStreak = 0;
					}
				}
				else
				{
					sb.Append(str[i]);
				}
			}

			return sb.ToString();
		}
	}
}