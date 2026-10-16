#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// One load and decode shared by every source that opens the same key through the bitmap cache, as WinUI's ImageCache is
/// shared by the BitmapImages that use it (MUX Reference: ImageProvider::EnsureCacheEntry — ImageProvider.cpp). It is
/// released like a decode a source owns once nothing displays it (see <see cref="PendingImageReleases"/>), and leaves the
/// cache then, so the next open decodes it again. UI thread only, except <see cref="Load"/>.
/// </summary>
internal sealed class SharedImageDecode
{
	// Not owning, as WinUI's provider store does not own its ImageCache (MUX Reference: ImageProvider::GetImageCache —
	// ImageProvider.cpp): a decode lives while an open waits for it, a source holds it or it waits for its release.
	private static readonly Dictionary<object, WeakReference<SharedImageDecode>> _decodes = new();
	private static int _sweepAt = 64;

	// The decode each shared surface comes from, so a subscriber that comes back still drawing one can use it again.
	private static readonly ConditionalWeakTable<ICompositionSurface, SharedImageDecode> _bySurface = new();

	private readonly object _key;

	// The opens waiting for the result. Each hands its use over to its source with the result.
	private int _opens;

	// The uses of the sources whose subscribers show it, each holding its source weakly: a source collected while still
	// subscribed (an ImageBrush keeps its subscription for its lifetime) never ends its use, so a dead one is dropped once it
	// is found. A source that keeps the result while nothing subscribes to it is not one: it opens its source again if the
	// result was released. In the order they began, so a scan stops at the first live one and drops the dead ones before it.
	private readonly LinkedList<SourceUse> _sources = new();

	private PendingImageReleases.Entry? _pendingRelease;

	private SharedImageDecode(object key, Func<Task<ImageData>> load)
	{
		_key = key;

		// Never on a requester's token, so cancelling one does not fail the others, as WinUI keeps the download running when
		// a request is released (MUX Reference: ImageCache::OnRequestReleasing — ImageCache.cpp).
		Load = Task.Run(async () =>
		{
			ImageData data;
			try
			{
				data = await load();
			}
			catch (Exception e)
			{
				data = ImageData.FromError(e);
			}

			if (data.CompositionSurface is not CompositionImageSurface surface)
			{
				return data;
			}

			_bySurface.AddOrUpdate(surface, this);
			return data.AsShared(this);
		});

		_ = Load.ContinueWith(
			static (_, state) => NativeDispatcher.Main.Enqueue(((SharedImageDecode)state!).OnLoadCompleted),
			this,
			CancellationToken.None,
			TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default);
	}

	/// <summary>
	/// The shared load. A successful result is marked with this decode (<see cref="ImageData.Share"/>).
	/// </summary>
	internal Task<ImageData> Load { get; }

	/// <summary>
	/// Whether the decoded pixels were released because nothing displayed them.
	/// </summary>
	internal bool IsReleased { get; private set; }

	/// <summary>
	/// The pixel size of the decode, once its pixels are released.
	/// </summary>
	internal global::System.Numerics.Vector2? ReleasedPixelSize { get; private set; }

	// A failed load must not keep failing later requesters: WinUI drops an ImageCache once released, so the next request
	// downloads again (MUX Reference: ImageProvider::OnImageCacheInvalidated — ImageProvider.cpp).
	private bool IsFailed => Load.IsCompleted && Load.Result.Share != this;

	/// <summary>
	/// Returns the decode shared for <paramref name="key"/>, starting it with <paramref name="load"/> when there is none.
	/// </summary>
	internal static SharedImageDecode GetOrStart(object key, Func<Task<ImageData>> load)
	{
		// A lookup can see a load that just failed before its completion was handled (e.g. an immediate retry from
		// ImageFailed); treat it as a miss so the retry loads again.
		if (!TryGet(key, out var decode) || decode.IsFailed)
		{
			decode = new SharedImageDecode(key, load);
			_decodes[key] = new(decode);
			SweepCollected();
		}

		return decode;
	}

	internal static Task<ImageData>? GetLoadForTesting(object key) => TryGet(key, out var decode) ? decode.Load : null;

	/// <summary>
	/// The shared data drawn from <paramref name="surface"/>, unless it was released.
	/// </summary>
	internal static ImageData FindLive(ICompositionSurface surface)
		=> _bySurface.TryGetValue(surface, out var decode) && !decode.IsReleased && decode.Load.IsCompletedSuccessfully ? decode.Load.Result : default;

	private static bool TryGet(object key, [NotNullWhen(true)] out SharedImageDecode? decode)
	{
		decode = null;
		return _decodes.TryGetValue(key, out var weak) && weak.TryGetTarget(out decode);
	}

	// Drops the keys of collected decodes, at most once each time the table doubles.
	private static void SweepCollected()
	{
		if (_decodes.Count >= _sweepAt)
		{
			foreach (var (key, weak) in _decodes)
			{
				if (!weak.TryGetTarget(out _))
				{
					_decodes.Remove(key);
				}
			}

			_sweepAt = Math.Max(64, _decodes.Count * 2);
		}
	}

	/// <summary>
	/// Counts an open that waits for the result, which keeps it from being released until <see cref="EndOpen"/>.
	/// </summary>
	internal void BeginOpen()
	{
		_opens++;
		CancelPendingRelease();
	}

	internal void EndOpen()
	{
		_opens--;
		ScheduleReleaseIfUnused();
	}

	/// <summary>
	/// A source's use of the result, which keeps it from being released until <see cref="RemoveSource"/> ends it, or until
	/// the source is collected.
	/// </summary>
	internal sealed class SourceUse(WeakReference<ImageSource> source)
	{
		private readonly WeakReference<ImageSource> _source = source;

		internal LinkedListNode<SourceUse>? Node;

		internal bool IsAlive => _source.TryGetTarget(out _);
	}

	internal SourceUse AddSource(WeakReference<ImageSource> source)
	{
		var use = new SourceUse(source);
		use.Node = _sources.AddLast(use);
		CancelPendingRelease();
		return use;
	}

	internal void RemoveSource(SourceUse use)
	{
		if (use.Node is { List: not null } node)
		{
			_sources.Remove(node);
		}

		ScheduleReleaseIfUnused();
	}

	// Drops the dead uses in front, each once, and stops at the first live one.
	private bool HasLiveSource()
	{
		while (_sources.First is { } first)
		{
			if (first.Value.IsAlive)
			{
				return true;
			}

			_sources.RemoveFirst();
		}

		return false;
	}

	private void CancelPendingRelease()
	{
		if (_pendingRelease is { } pending)
		{
			PendingImageReleases.Remove(pending);
			_pendingRelease = null;
		}
	}

	private void OnLoadCompleted()
	{
		if (IsFailed)
		{
			RemoveFromCache();
		}
		else
		{
			// A load whose requesters were all cancelled still produces its result, which then waits like any other.
			ScheduleReleaseIfUnused();
		}
	}

	private void ScheduleReleaseIfUnused()
	{
		if (_opens == 0 && _pendingRelease is null && !IsReleased && Load.IsCompleted && !IsFailed && !HasLiveSource())
		{
			_pendingRelease = PendingImageReleases.Add(Surface.DecodedByteCount, Release);
		}
	}

	private void Release()
	{
		_pendingRelease = null;
		IsReleased = true;
		ReleasedPixelSize = Surface.Size;
		RemoveFromCache();
		Surface.ReleaseFrames();
	}

	private CompositionImageSurface Surface => (CompositionImageSurface)Load.Result.CompositionSurface!;

	private void RemoveFromCache()
	{
		// Only while it is still the stored one (MUX Reference: ImageProvider::RemoveImageCache — ImageProvider.cpp).
		if (TryGet(_key, out var stored) && stored == this)
		{
			_decodes.Remove(_key);
		}
	}
}
