#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Buffers;
using Uno.Foundation.Logging;
using Windows.System;

namespace Uno.Helpers
{
	internal class LRUCache<TKey, TValue> where TKey : notnull
	{
		private readonly int _maxEntryCount;
		private readonly Logger _log = typeof(LRUCache<TKey, TValue>).Log();
		private readonly Stopwatch _watch = Stopwatch.StartNew();
		private readonly Dictionary<TKey, LinkedListNode<KeyEntry>> _table = new();
		private readonly LinkedList<KeyEntry> _queue = new();
		private readonly object _gate = new();

		private TimeSpan _lastScavenge;

		private readonly TimeSpan LowMemoryTrimInterval = TimeSpan.FromMinutes(5);
		private readonly TimeSpan MediumMemoryTrimInterval = TimeSpan.FromMinutes(3);
		private readonly TimeSpan HighMemoryTrimInterval = TimeSpan.FromMinutes(1);
		private readonly TimeSpan OverLimitMemoryTrimInterval = TimeSpan.FromMinutes(.5);
		private readonly TimeSpan ScavengeInterval = TimeSpan.FromMinutes(.5);

		private readonly DefaultArrayPoolPlatformProvider _platformProvider = new();

		/// <summary>Determines if automatic memory management is enabled</summary>
		private readonly bool _automaticManagement;

		private readonly record struct KeyEntry(TKey key, TValue val, TimeSpan LastUse);

		public LRUCache(int maxEntryCount)
		{
			_maxEntryCount = maxEntryCount;
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
		/// Gets a potentially cached value for the given key
		/// </summary>
		public bool TryGetFromKey(TKey key, out TValue? value)
		{
			Scavenge();

			lock (_gate)
			{
				if (_table.TryGetValue(key, out var result))
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Reusing cached value for [{key}]");
					}

					var entry = result.Value;
					result.Value = entry with { LastUse = _watch.Elapsed };
					_queue.Remove(result);
					_queue.AddFirst(result);

					value = entry.val;
					return true;
				}
				else
				{
					value = default;
					return false;
				}
			}
		}

		public void Remove(TKey key)
		{
			lock (_gate)
			{
				if (_table.Remove(key, out var node))
				{
					_queue.Remove(node);
				}
			}
		}

		public void Add(TKey key, TValue value)
		{
			lock (_gate)
			{
				if (_queue.Count == _maxEntryCount)
				{
					var last = _queue.Last!.Value.key;
					_table.Remove(last);
					_queue.RemoveLast();

					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"{typeof(TValue).Name} is full. Evicting [{last}]");
					}
				}

				if (_log.IsEnabled(LogLevel.Trace))
				{
					_log.Trace($"Caching key [{key}]");
				}

				var node = new LinkedListNode<KeyEntry>(new KeyEntry(key, value, _watch.Elapsed));
				_queue.AddFirst(node);
				_table[key] = node;
			}
		}

		private bool Trim()
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

		private void Scavenge()
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

		private void Trim(TimeSpan interval)
		{
			lock (_gate)
			{
				int trimmedCount = 0;

				foreach (var entry in _table.Values)
				{
					var node = entry.Value;
					if (node.LastUse + interval < _watch.Elapsed)
					{
						_table.Remove(node.key);
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
