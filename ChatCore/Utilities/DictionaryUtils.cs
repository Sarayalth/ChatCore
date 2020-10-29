using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ChatCore.Utilities
{
	public static class DictionaryUtils
	{
		public static void AddAction(this ConcurrentDictionary<Assembly, Action> dict, Assembly assembly, Action value)
		{
			dict.AddOrUpdate(assembly, value, (_, existingActions) => existingActions + value);
		}

		public static void AddAction<TA>(this ConcurrentDictionary<Assembly, Action<TA>> dict, Assembly assembly, Action<TA> value)
		{
			dict.AddOrUpdate(assembly, value, (_, existingActions) => existingActions + value);
		}

		public static void AddAction<TA, TB>(this ConcurrentDictionary<Assembly, Action<TA, TB>> dict, Assembly assembly, Action<TA, TB> value)
		{
			dict.AddOrUpdate(assembly, value, (_, existingActions) => existingActions + value);
		}

		public static void AddAction<TA, TB, TC>(this ConcurrentDictionary<Assembly, Action<TA, TB, TC>> dict, Assembly assembly, Action<TA, TB, TC> value)
		{
			dict.AddOrUpdate(assembly, value, (_, existingActions) => existingActions + value);
		}

		public static void RemoveAction(this ConcurrentDictionary<Assembly, Action> dict, Assembly assembly, Action? value)
		{
			if (!dict.TryGetValue(assembly, out var compoundAction))
			{
				return;
			}

			compoundAction -= value;
			dict[assembly] = compoundAction!;
		}

		public static void RemoveAction<TA>(this ConcurrentDictionary<Assembly, Action<TA>> dict, Assembly assembly, Action<TA> value)
		{
			if (!dict.TryGetValue(assembly, out var compoundAction))
			{
				return;
			}

			compoundAction -= value;
			dict[assembly] = compoundAction!;
		}

		public static void RemoveAction<TA, TB>(this ConcurrentDictionary<Assembly, Action<TA, TB>> dict, Assembly assembly, Action<TA, TB> value)
		{
			if (!dict.TryGetValue(assembly, out var compoundAction))
			{
				return;
			}


			compoundAction -= value;
			dict[assembly] = compoundAction!;
		}

		public static void RemoveAction<TA, TB, TC>(this ConcurrentDictionary<Assembly, Action<TA, TB, TC>> dict, Assembly assembly, Action<TA, TB, TC> value)
		{
			if (dict.TryGetValue(assembly, out var compoundAction))
			{
				compoundAction -= value;
				dict[assembly] = compoundAction!;
			}
		}

		public static void InvokeAll(this ConcurrentDictionary<Assembly, Action> dict, Assembly assembly, ILogger? logger = null)
		{
			foreach (var kvp in dict)
			{
				if (kvp.Key == assembly)
				{
					continue;
				}

				try
				{
					kvp.Value?.Invoke();
				}
				catch (Exception ex)
				{
					logger?.LogError(ex, $"An exception occurred while invoking action no params.");
				}
			}
		}

		public static void InvokeAll<TA>(this ConcurrentDictionary<Assembly, Action<TA>> dict, Assembly assembly, TA a, ILogger? logger = null)
		{
			foreach (var kvp in dict)
			{
				if (kvp.Key == assembly)
				{
					continue;
				}

				try
				{
					kvp.Value?.Invoke(a);
				}
				catch (Exception ex)
				{
					logger?.LogError(ex, $"An exception occurred while invoking action with param type {typeof(TA).Name}");
				}
			}
		}

		public static void InvokeAll<TA, TB>(this ConcurrentDictionary<Assembly, Action<TA, TB>> dict, Assembly assembly, TA a, TB b, ILogger? logger = null)
		{
			foreach (var kvp in dict)
			{
				if (kvp.Key == assembly)
				{
					continue;
				}

				try
				{
					kvp.Value?.Invoke(a, b);
				}
				catch (Exception ex)
				{
					logger?.LogError(ex, $"An exception occurred while invoking action with param types {typeof(TA).Name}, {typeof(TB).Name}");
				}
			}
		}

		public static void InvokeAll<TA, TB, TC>(this ConcurrentDictionary<Assembly, Action<TA, TB, TC>> dict, Assembly assembly, TA a, TB b, TC c, ILogger? logger = null)
		{
			foreach (var kvp in dict)
			{
				if (kvp.Key == assembly)
				{
					continue;
				}

				try
				{
					kvp.Value?.Invoke(a, b, c);
				}
				catch (Exception ex)
				{
					logger?.LogError(ex, $"An exception occurred while invoking action with param types {typeof(TA).Name}, {typeof(TB).Name}, {typeof(TC).Name}");
				}
			}
		}
	}
}