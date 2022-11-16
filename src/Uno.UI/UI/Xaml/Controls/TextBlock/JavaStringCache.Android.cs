#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Text;
using Windows.Foundation;

using Uno;
using Uno.Extensions;
using Uno.UI;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media;
using Uno.Collections;
using Android.Security.Keystore;
using Java.Security;
using Uno.Buffers;
using Windows.System;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A TextBlock measure cache for non-formatted text.
	/// </summary>
	internal static class JavaStringCache
	{
		private static Logger _log = typeof(JavaStringCache).Log();
		private static Stopwatch _watch = Stopwatch.StartNew();
		private static HashtableEx _table = new();
		private static TimeSpan _lastScavenge;
		private static object _gate = new();

		internal static readonly TimeSpan LowMemoryTrimInterval = TimeSpan.FromMinutes(5);
		internal static readonly TimeSpan MediumMemoryTrimInterval = TimeSpan.FromMinutes(3);
		internal static readonly TimeSpan HighMemoryTrimInterval = TimeSpan.FromMinutes(1);
		internal static readonly TimeSpan OverLimitMemoryTrimInterval = TimeSpan.FromMinutes(.5);

		internal static readonly TimeSpan ScavengeInterval = TimeSpan.FromMinutes(.5);

		private static DefaultArrayPoolPlatformProvider _platformProvider = new DefaultArrayPoolPlatformProvider();

		/// <summary>Determines if automatic memory management is enabled</summary>
		private static readonly bool _automaticManagement;
		/// <summary>Determines if GC trim callback has been registerd if non-zero</summary>
		private static int _trimCallbackCreated;

		private record KeyEntry(string Value, Java.Lang.String NativeValue)
		{
			public TimeSpan LastUse { get; set; } = _watch.Elapsed;
		}

		static JavaStringCache()
		{
			_automaticManagement = WinRTFeatureConfiguration.ArrayPool.EnableAutomaticMemoryManagement && _platformProvider.CanUseMemoryManager;
		}

		/// <summary>
		/// Gets a potentially cached native instance of a .NET string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Java.Lang.String GetNativeString(string value)
		{
			TryInitializeMemoryManagement();

			Scavenge();

			lock (_gate)
			{
				if (_table.TryGetValue(value, out var result) && result is KeyEntry entry)
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Reusing native string: [{value}]");
					}

					entry.LastUse = _watch.Elapsed;
					return entry.NativeValue;
				}
				else
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Creating native string for [{value}]");
					}

					var javaString = new Java.Lang.String(value);
					_table[value] = new KeyEntry(value, javaString);
					return javaString;
				}
			}
		}

		private static void TryInitializeMemoryManagement()
		{
			if (_automaticManagement && Interlocked.Exchange(ref _trimCallbackCreated, 1) == 0)
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

		private static bool Trim()
		{
			if (!_automaticManagement)
			{
				return false;
			}

			var threshold = _platformProvider?.AppMemoryUsageLevel switch
			{
				AppMemoryUsageLevel.Low => LowMemoryTrimInterval,
				AppMemoryUsageLevel.Medium => MediumMemoryTrimInterval,
				AppMemoryUsageLevel.High => HighMemoryTrimInterval,
				AppMemoryUsageLevel.OverLimit => OverLimitMemoryTrimInterval,
				_ => LowMemoryTrimInterval
			};

			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"Memory pressure is {_platformProvider?.AppMemoryUsageLevel}, using trim interval of {threshold}");
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
				List<string>? entries = null;
				foreach (var entry in _table.Values)
				{
					if (entry is KeyEntry keyEntry && keyEntry.LastUse + interval < _watch.Elapsed)
					{
						entries ??= new();
						entries.Add(keyEntry.Value);
					}
				}

				if (entries is not null)
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.Debug($"Trimming {entries.Count} native strings unused since {interval}");
					}

					foreach (var entry in entries)
					{
						_table.Remove(entry);
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
