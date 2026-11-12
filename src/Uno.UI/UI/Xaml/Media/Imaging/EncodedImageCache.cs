#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Uno.Buffers;
using Uno.UI;
using Windows.System;

namespace Uno.Helpers;

/// <summary>
/// The encoded bytes of recently downloaded images, so that a BitmapImage that downloaded none of its own, or no longer has
/// them, does not download them again to decode them. A WinUI BitmapImage keeps them only while it uses its ImageCache (as
/// Uno's keeps its own, see BitmapImage), and a later request downloads again through the system HTTP stack, whose cache
/// follows the caching headers (MUX Reference: ImageCache::Download — ImageCache.cpp); Uno's HttpClient has no such cache
/// on desktop. Bounded by <see cref="FeatureConfiguration.Image.MaxBitmapImageCacheBytes"/>, the least recently used going
/// first; bytes unused for <see cref="IdleExpiry"/> are dropped, sooner under memory pressure (not on iOS or tvOS, where
/// MemoryManager isn't available), and the bytes of a response are not used once it is stale. Thread-safe.
/// </summary>
internal static class EncodedImageCache
{
	private static readonly object _gate = new();
	private static readonly Dictionary<Uri, LinkedListNode<Entry>> _entries = new();
	private static readonly LinkedList<Entry> _recentFirst = new();
	private static long _byteCount;

	// Drops bytes once they expire, also while nothing uses the cache.
	private static Timer? _expiryTimer;

	// Checks the memory pressure at each gen 2 collection, as the bitmap cache this replaces did, unless the app turned
	// automatic memory management off. Null until bytes are kept.
	private static DefaultArrayPoolPlatformProvider? _memory;

	private sealed class Entry(Uri uri, byte[] bytes, long? freshUntil)
	{
		public Uri Uri { get; } = uri;

		public byte[] Bytes { get; } = bytes;

		// When the response stops being fresh (a Stopwatch timestamp), if its headers give a lifetime.
		public long? FreshUntil { get; } = freshUntil;

		public long LastUse { get; set; } = Stopwatch.GetTimestamp();
	}

	/// <summary>
	/// How long bytes nothing used are kept, as the bitmap cache this replaces dropped entries unused for 5 minutes. Tests
	/// override it.
	/// </summary>
	internal static TimeSpan IdleExpiry { get; set; } = TimeSpan.FromMinutes(5);

	internal static long ByteCountForTesting
	{
		get
		{
			lock (_gate)
			{
				return _byteCount;
			}
		}
	}

	/// <summary>
	/// Whether the bytes of <paramref name="uri"/> are kept: app assets and local files are cheap to read again.
	/// </summary>
	internal static bool CanCache(Uri uri) => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

	internal static bool TryGet(Uri uri, [NotNullWhen(true)] out byte[]? bytes)
	{
		lock (_gate)
		{
			Trim();

			if (_entries.TryGetValue(uri, out var node))
			{
				var now = Stopwatch.GetTimestamp();

				// Bytes no longer fresh are not used.
				if (node.Value.FreshUntil <= now)
				{
					_entries.Remove(uri);
					RemoveNode(node);
					bytes = null;
					return false;
				}

				node.Value.LastUse = now;
				_recentFirst.Remove(node);
				_recentFirst.AddFirst(node);
				bytes = node.Value.Bytes;
				return true;
			}
		}

		bytes = null;
		return false;
	}

	/// <param name="reusableFor">How long the response may be reused, or <see cref="Timeout.InfiniteTimeSpan"/>.</param>
	internal static void Add(Uri uri, byte[] bytes, TimeSpan reusableFor)
	{
		lock (_gate)
		{
			// A download in flight when the cache was turned off does not refill it: checked under the lock, so it either
			// precedes the Clear that turning it off runs or sees it off.
			if (!FeatureConfiguration.Image.EnableBitmapImageCache)
			{
				return;
			}

			if (_entries.Remove(uri, out var previous))
			{
				RemoveNode(previous);
			}

			// An image larger than the cache would only evict the others.
			if (bytes.Length <= FeatureConfiguration.Image.MaxBitmapImageCacheBytes)
			{
				// A lifetime past a year is as good as none, and would overflow the timestamp.
				var freshUntil = reusableFor == Timeout.InfiniteTimeSpan || reusableFor > TimeSpan.FromDays(365)
					? (long?)null
					: Stopwatch.GetTimestamp() + (long)(reusableFor.TotalSeconds * Stopwatch.Frequency);
				_entries[uri] = _recentFirst.AddFirst(new Entry(uri, bytes, freshUntil));
				_byteCount += bytes.Length;
				WatchMemory();
			}

			Trim();
		}
	}

	/// <summary>
	/// Drops the least recently used bytes past the bound, and those nothing used for <see cref="IdleExpiry"/>. Bytes no
	/// longer fresh are not used (see <see cref="TryGet"/>), so they leave that way.
	/// </summary>
	internal static void Trim() => Trim(IdleExpiry);

	/// <summary>
	/// Drops bytes sooner the higher the memory pressure, in the proportions the bitmap cache this replaces used (5, 3, 1
	/// and 0.5 minutes).
	/// </summary>
	internal static void TrimForMemoryPressure(AppMemoryUsageLevel level)
		=> Trim(level switch
		{
			AppMemoryUsageLevel.Medium => IdleExpiry * 0.6,
			AppMemoryUsageLevel.High => IdleExpiry * 0.2,
			AppMemoryUsageLevel.OverLimit => IdleExpiry * 0.1,
			_ => IdleExpiry,
		});

	private static void Trim(TimeSpan expiry)
	{
		lock (_gate)
		{
			var now = Stopwatch.GetTimestamp();
			while (_recentFirst.Last is { } last
				&& (_byteCount > FeatureConfiguration.Image.MaxBitmapImageCacheBytes || Stopwatch.GetElapsedTime(last.Value.LastUse, now) > expiry))
			{
				_entries.Remove(last.Value.Uri);
				RemoveNode(last);
			}

			// The least recently used bytes expire first: once they are due, the timer trims again.
			if (_recentFirst.Last is { } oldest)
			{
				var due = IdleExpiry - Stopwatch.GetElapsedTime(oldest.Value.LastUse, now);
				ScheduleTrim(due > TimeSpan.Zero ? due : TimeSpan.Zero);
			}
			else
			{
				_expiryTimer?.Change(Timeout.Infinite, Timeout.Infinite);
			}
		}
	}

	private static void ScheduleTrim(TimeSpan dueIn)
	{
		if (_expiryTimer is null)
		{
			// Not capturing the AsyncLocals of whichever caller came first.
			var flow = ExecutionContext.IsFlowSuppressed() ? default(AsyncFlowControl?) : ExecutionContext.SuppressFlow();
			try
			{
				_expiryTimer = new Timer(static _ => Trim());
			}
			finally
			{
				flow?.Undo();
			}
		}

		// Just past it, as what is due at that time is dropped once its time is passed.
		_expiryTimer.Change(dueIn + TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
	}

	private static void WatchMemory()
	{
		if (_memory is not null)
		{
			return;
		}

		// Kept alive here: the callback stops once its target is collected.
		var memory = _memory = new DefaultArrayPoolPlatformProvider();
		if (WinRTFeatureConfiguration.ArrayPool.EnableAutomaticMemoryManagement && memory.CanUseMemoryManager)
		{
			memory.RegisterTrimCallback(
				static target =>
				{
					// The timer drops what expires at low pressure.
					if (((DefaultArrayPoolPlatformProvider)target).AppMemoryUsageLevel is > AppMemoryUsageLevel.Low and var level)
					{
						TrimForMemoryPressure(level);
					}

					return true;
				},
				memory);
		}
	}

	internal static void Clear()
	{
		lock (_gate)
		{
			_entries.Clear();
			_recentFirst.Clear();
			_byteCount = 0;
			_expiryTimer?.Change(Timeout.Infinite, Timeout.Infinite);
		}
	}

	private static void RemoveNode(LinkedListNode<Entry> node)
	{
		_recentFirst.Remove(node);
		_byteCount -= node.Value.Bytes.Length;
	}
}
