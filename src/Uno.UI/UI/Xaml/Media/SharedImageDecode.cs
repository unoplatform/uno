#nullable enable

using System;
using System.Collections.Concurrent;
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
/// cache then, so the next open decodes it again. UI thread only, except <see cref="Load"/> and the finalizer of
/// <see cref="User"/>.
/// </summary>
internal sealed class SharedImageDecode
{
	// Not owning, as WinUI's provider store does not own its ImageCache (MUX Reference: ImageProvider::GetImageCache —
	// ImageProvider.cpp): a decode lives while an open waits for it, a source holds it or it waits for its release.
	private static readonly Dictionary<object, WeakReference<SharedImageDecode>> _decodes = new();
	private static int _sweepAt = 64;

	// The decode each shared surface comes from, so a subscriber that comes back still drawing one can use it again.
	private static readonly ConditionalWeakTable<ICompositionSurface, SharedImageDecode> _bySurface = new();

	// The uses of collected sources, to end on the UI thread (see User), and whether ending them is posted.
	private static readonly ConcurrentQueue<SourceUse> _collectedUses = new();
	private static int _collectedUsesPosted;

	private readonly object _key;

	// The opens waiting for the result. Each hands its use over to its source with the result.
	private int _opens;

	// The uses of the sources whose subscribers show it, each ended by its source once they let it go, or by its User once
	// the source is collected (an ImageBrush keeps its subscription for its lifetime). A source that keeps the result while
	// nothing subscribes to it is not one: it opens its source again if the result was released.
	private readonly LinkedList<SourceUse> _sources = new();

	private PendingImageReleases.Entry? _pendingRelease;

	// What the uses refer to it by, so that the uses a collected source leaves don't keep it alive.
	private WeakReference<SharedImageDecode>? _weakSelf;

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
	/// A source's use of the result, which keeps it from being released until <see cref="RemoveSource"/> ends it.
	/// </summary>
	internal sealed class SourceUse(WeakReference<SharedImageDecode> decode)
	{
		internal readonly WeakReference<SharedImageDecode> Decode = decode;

		internal LinkedListNode<SourceUse>? Node;
	}

	/// <summary>
	/// What a source is known by in the decodes it uses. Only the source holds it, so it is collected with the source, which
	/// can no longer end its uses: it then has them ended on the UI thread, as the source would have.
	/// </summary>
	internal sealed class User
	{
		internal readonly List<SourceUse> Uses = new(1);

		~User()
		{
			// On the finalizer thread, it only queues them, posting once for any number of collected users.
			foreach (var use in Uses)
			{
				_collectedUses.Enqueue(use);
			}

			if (Uses.Count > 0 && Interlocked.Exchange(ref _collectedUsesPosted, 1) == 0)
			{
				try
				{
					NativeDispatcher.Main.Enqueue(EndCollectedUses);
				}
				catch (Exception)
				{
					// No UI thread to post to, e.g. while the app shuts down: they wait for the next post.
					Volatile.Write(ref _collectedUsesPosted, 0);
				}
			}
		}
	}

	internal SourceUse AddSource(User user)
	{
		var use = new SourceUse(_weakSelf ??= new(this));
		use.Node = _sources.AddLast(use);
		user.Uses.Add(use);
		CancelPendingRelease();
		return use;
	}

	internal void RemoveSource(SourceUse use, User? user)
	{
		user?.Uses.Remove(use);
		if (use.Node is { List: not null } node)
		{
			_sources.Remove(node);
		}

		ScheduleReleaseIfUnused();
	}

	private static void EndCollectedUses()
	{
		Volatile.Write(ref _collectedUsesPosted, 0);
		while (_collectedUses.TryDequeue(out var use))
		{
			// Unless nothing reaches the decode either: it is then collected, with its frames.
			if (use.Decode.TryGetTarget(out var decode))
			{
				decode.RemoveSource(use, null);
			}
		}
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
		if (_opens == 0 && _pendingRelease is null && !IsReleased && Load.IsCompleted && !IsFailed && _sources.Count == 0)
		{
			_pendingRelease = PendingImageReleases.Add(Surface.DecodedByteCount, Release);
		}
	}

	private void Release()
	{
		_pendingRelease = null;
		IsReleased = true;
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
