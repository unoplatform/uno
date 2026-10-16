#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Uno.UI;

namespace Uno.Helpers;

/// <summary>
/// The encoded bytes of recently downloaded images, so that an image decoded again after its pixels were released is not
/// downloaded again. WinUI keeps them only while a BitmapImage uses its ImageCache, and a later request downloads again
/// through the system HTTP stack, whose cache follows the caching headers (MUX Reference: ImageCache::Download —
/// ImageCache.cpp); Uno's HttpClient has no such cache on desktop. Bounded by
/// <see cref="FeatureConfiguration.Image.MaxBitmapImageCacheBytes"/>, the least recently used going first, and bytes
/// unused for <see cref="IdleExpiry"/> are dropped. Thread-safe.
/// </summary>
internal static class EncodedImageCache
{
	private static readonly object _gate = new();
	private static readonly Dictionary<Uri, LinkedListNode<Entry>> _entries = new();
	private static readonly LinkedList<Entry> _recentFirst = new();
	private static long _byteCount;

	private sealed class Entry(Uri uri, byte[] bytes)
	{
		public Uri Uri { get; } = uri;

		public byte[] Bytes { get; } = bytes;

		public long LastUse { get; set; } = Stopwatch.GetTimestamp();
	}

	/// <summary>
	/// How long bytes nothing used are kept, as the bitmap cache this replaces dropped entries unused for 5 minutes.
	/// Checked whenever the cache is used. Tests override it.
	/// </summary>
	internal static TimeSpan IdleExpiry { get; set; } = TimeSpan.FromMinutes(5);

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
		// A download in flight when the cache was turned off does not refill it.
		if (!FeatureConfiguration.Image.EnableBitmapImageCache)
		{
			return;
		}

		lock (_gate)
		{
			if (_entries.Remove(uri, out var previous))
			{
				RemoveNode(previous);
			}

			// An image larger than the cache would only evict the others.
			if (bytes.Length <= FeatureConfiguration.Image.MaxBitmapImageCacheBytes)
			{
				_entries[uri] = _recentFirst.AddFirst(new Entry(uri, bytes));
				_byteCount += bytes.Length;
			}

			Trim();
		}
	}

	/// <summary>
	/// Drops the least recently used bytes past the bound, and those nothing used for <see cref="IdleExpiry"/>.
	/// </summary>
	internal static void Trim()
	{
		lock (_gate)
		{
			var now = Stopwatch.GetTimestamp();
			while (_recentFirst.Last is { } last
				&& (_byteCount > FeatureConfiguration.Image.MaxBitmapImageCacheBytes || Stopwatch.GetElapsedTime(last.Value.LastUse, now) > IdleExpiry))
			{
				_entries.Remove(last.Value.Uri);
				RemoveNode(last);
			}
		}
	}

	internal static void Clear()
	{
		lock (_gate)
		{
			_entries.Clear();
			_recentFirst.Clear();
			_byteCount = 0;
		}
	}

	private static void RemoveNode(LinkedListNode<Entry> node)
	{
		_recentFirst.Remove(node);
		_byteCount -= node.Value.Bytes.Length;
	}
}
