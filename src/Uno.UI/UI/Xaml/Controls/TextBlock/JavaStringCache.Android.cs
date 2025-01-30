#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Uno;
using Uno.Foundation.Logging;
using Uno.Buffers;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// A cache for native java strings. This cache periodically evicts entries that haven't been used
	/// in a while. Additionally, it also evicts the least recently used entries when adding new entries beyond a certain
	/// capacity. Limiting the total capacity is necessary to deal with the Android-limited GREF counts.
	/// </summary>
	internal static class JavaStringCache
	{
		// Xamarin.Android uses Android global references to provide mappings between Java instances and the associated managed instances, as when invoking a Java method a Java instance needs to be provided to Java.
		// Unfortunately, Android emulators only allow 2000 global references to exist at a time. Hardware has a much higher limit of 52000 global references. The lower limit can be problematic when running applications on the emulator, so knowing where the instance came from can be very useful.
		// https://github.com/MicrosoftDocs/xamarin-docs/blob/live/docs/android/troubleshooting/troubleshooting.md
		// https://github.com/unoplatform/uno/issues/18951
		private static readonly int _maxEntryCount = Uno.UI.FeatureConfiguration.TextBlock.JavaStringCachedCapacity;
		private static readonly Logger _log = typeof(JavaStringCache).Log();
		private static readonly Stopwatch _watch = Stopwatch.StartNew();
		private static readonly Dictionary<string, LinkedListNode<KeyEntry>> _table = new();
		private static readonly LinkedList<KeyEntry> _queue = new();
		private static readonly object _gate = new();

		private static TimeSpan _lastScavenge;

		internal static readonly TimeSpan LowMemoryTrimInterval = TimeSpan.FromMinutes(5);
		internal static readonly TimeSpan MediumMemoryTrimInterval = TimeSpan.FromMinutes(3);
		internal static readonly TimeSpan HighMemoryTrimInterval = TimeSpan.FromMinutes(1);
		internal static readonly TimeSpan OverLimitMemoryTrimInterval = TimeSpan.FromMinutes(.5);

		internal static readonly TimeSpan ScavengeInterval = TimeSpan.FromMinutes(.5);

		private static readonly DefaultArrayPoolPlatformProvider _platformProvider = new DefaultArrayPoolPlatformProvider();

		/// <summary>Determines if automatic memory management is enabled</summary>
		private static readonly bool _automaticManagement;

		private readonly record struct KeyEntry(string CsString, Java.Lang.String JavaString, TimeSpan LastUse);

		static JavaStringCache()
		{
			_automaticManagement = WinRTFeatureConfiguration.ArrayPool.EnableAutomaticMemoryManagement && _platformProvider.CanUseMemoryManager;
			if (_automaticManagement)
			{
				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.Debug($"Using automatic memory management");
				}

				_platformProvider.RegisterTrimCallback(_ => Trim(), _gate);
			}
			else
			{
				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.Debug($"Using manual memory management");
				}
			}
		}

		/// <summary>
		/// Gets a potentially cached native instance of a .NET string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Java.Lang.String GetNativeString(string value)
		{
			Scavenge();

			lock (_gate)
			{
				if (_table.TryGetValue(value, out var result))
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Reusing native string: [{value}]");
					}

					var entry = result.Value;
					result.Value = entry with { LastUse = _watch.Elapsed };
					_queue.Remove(result);
					_queue.AddFirst(result);

					return entry.JavaString;
				}
				else
				{
					if (_queue.Count == _maxEntryCount)
					{
						var last = _queue.Last!.Value.CsString;
						_table.Remove(last);
						_queue.RemoveLast();

						if (_log.IsEnabled(LogLevel.Trace))
						{
							_log.Trace($"{nameof(JavaStringCache)} is full. Evicting [{last}]");
						}
					}

					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Creating native string for [{value}]");
					}

					var javaString = new Java.Lang.String(value);
					var node = new LinkedListNode<KeyEntry>(new KeyEntry(value, javaString, _watch.Elapsed));
					_queue.AddFirst(node);
					_table[value] = node;
					return javaString;
				}
			}
		}

		private static bool Trim()
		{
			if (!_automaticManagement)
			{
				return false;
			}

			var threshold = _platformProvider.AppMemoryUsageLevel switch
			{
				AppMemoryUsageLevel.Low => LowMemoryTrimInterval,
				AppMemoryUsageLevel.Medium => MediumMemoryTrimInterval,
				AppMemoryUsageLevel.High => HighMemoryTrimInterval,
				AppMemoryUsageLevel.OverLimit => OverLimitMemoryTrimInterval,
				_ => LowMemoryTrimInterval
			};

			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"Memory pressure is {_platformProvider.AppMemoryUsageLevel}, using trim interval of {threshold}");
			}

			Trim(threshold);

			return true;
		}

		private static void Scavenge()
		{
			if (!_automaticManagement)
			{
				if (_lastScavenge + ScavengeInterval < _watch.Elapsed)
				{
					_lastScavenge = _watch.Elapsed;
					Trim(LowMemoryTrimInterval);
				}
			}
		}

		private static void Trim(TimeSpan interval)
		{
			lock (_gate)
			{
				int trimmedCount = 0;
				foreach (var entry in _table.Values)
				{
					var node = entry.Value;
					if (node.LastUse + interval < _watch.Elapsed)
					{
						_table.Remove(node.CsString);
						_queue.Remove(node);
						trimmedCount++;
					}
				}

				if (trimmedCount > 0)
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.Debug($"Trimming {trimmedCount} native strings unused since {interval}");
					}
				}
				else
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Nothing to trim for the past {interval}");
					}
				}
			}
		}
	}
}
