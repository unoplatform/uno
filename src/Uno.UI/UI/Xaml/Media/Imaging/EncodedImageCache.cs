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
/// first, and bytes unused for <see cref="IdleExpiry"/> are dropped, sooner under memory pressure. Thread-safe.
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

	private sealed class Entry(Uri uri, byte[] bytes)
	{
		public Uri Uri { get; } = uri;

		public byte[] Bytes { get; } = bytes;

		public long LastUse { get; set; } = Stopwatch.GetTimestamp();
	}

	/// <summary>
	/// How long bytes nothing used are kept, as the bitmap cache this replaces dropped entries unused for 5 minutes. Tests
	/// override it.
	/// </summary>
	internal static TimeSpan IdleExpiry { get; set; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Whether the gen 2 callback trims for the memory pressure. Tests turn it off to see the expiry alone.
	/// </summary>
	internal static bool TrimsOnGen2 { get; set; } = true;

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
				node.Value.LastUse = Stopwatch.GetTimestamp();
				_recentFirst.Remove(node);
				_recentFirst.AddFirst(node);
				bytes = node.Value.Bytes;
				return true;
			}
		}

		bytes = null;
		return false;
	}

	internal static void Add(Uri uri, byte[] bytes)
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
				_entries[uri] = _recentFirst.AddFirst(new Entry(uri, bytes));
				_byteCount += bytes.Length;
				WatchMemory();
			}

			Trim();
		}
	}

	/// <summary>
	/// Drops the least recently used bytes past the bound, and those nothing used for <see cref="IdleExpiry"/>.
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
				(_expiryTimer ??= new Timer(static _ => Trim())).Change(
					(due > TimeSpan.Zero ? due : TimeSpan.Zero) + TimeSpan.FromMilliseconds(1),
					Timeout.InfiniteTimeSpan);
			}
			else
			{
				_expiryTimer?.Change(Timeout.Infinite, Timeout.Infinite);
			}
		}
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
					if (TrimsOnGen2)
					{
						TrimForMemoryPressure(((DefaultArrayPoolPlatformProvider)target).AppMemoryUsageLevel);
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
