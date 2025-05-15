#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno;
using Uno.Buffers;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.System;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage
	{
		internal static class BitmapImageCache
		{
			private static readonly int _maxEntryCount = Uno.UI.FeatureConfiguration.Image.MaxBitmapImageCacheCount;
			private static readonly Logger _log = typeof(BitmapImageCache).Log();
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

			private readonly record struct KeyEntry(string Uri, Task<ImageData> ImageTask, TimeSpan LastUse);

			static BitmapImageCache()
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
			/// Gets a potentially cached <see cref="ImageData"/> for the given uri
			/// </summary>
			public static bool TryGetFromUri(Uri uri, out Task<ImageData>? task)
			{
				Scavenge();

				lock (_gate)
				{
					if (_table.TryGetValue(uri.OriginalString, out var result))
					{
						if (_log.IsEnabled(LogLevel.Trace))
						{
							_log.Trace($"Reusing Bitmap for [{uri.OriginalString}]");
						}

						var entry = result.Value;
						result.Value = entry with { LastUse = _watch.Elapsed };
						_queue.Remove(result);
						_queue.AddFirst(result);

						task = entry.ImageTask;
						return true;
					}
					else
					{
						task = null;
						return false;
					}
				}
			}

			public static void Remove(Uri uri, Task<ImageData> task)
			{
				lock (_gate)
				{
					if (_table.Remove(uri.OriginalString, out var node))
					{
						_queue.Remove(node);
					}
				}
			}

			public static void Add(Uri uri, Task<ImageData> task)
			{
				var originalString = uri.OriginalString;

				lock (_gate)
				{
					if (_queue.Count == _maxEntryCount)
					{
						var last = _queue.Last!.Value.Uri;
						_table.Remove(last);
						_queue.RemoveLast();

						if (_log.IsEnabled(LogLevel.Trace))
						{
							_log.Trace($"{nameof(BitmapImageCache)} is full. Evicting [{last}]");
						}
					}

					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Caching image [{originalString}]");
					}

					var node = new LinkedListNode<KeyEntry>(new KeyEntry(originalString, task, _watch.Elapsed));
					_queue.AddFirst(node);
					_table[originalString] = node;
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
							_table.Remove(node.Uri);
							_queue.Remove(node);
							trimmedCount++;
						}
					}

					if (trimmedCount > 0)
					{
						if (_log.IsEnabled(LogLevel.Debug))
						{
							_log.Debug($"Trimming {trimmedCount} bitmap unused since {interval}");
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
}
