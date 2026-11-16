#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Decoded images nothing displays, kept for <see cref="ReleaseDelay"/> in case they are displayed again, and released
/// sooner, oldest first, while together they hold more than <see cref="FeatureConfiguration.Image.MaxUndisplayedImageBytes"/>.
/// Releases run at the end of a layout pass, so an image re-parented or re-templated during a pass keeps its pixels.
/// UI thread only.
/// </summary>
internal static class PendingImageReleases
{
	/// <summary>
	/// WinUI frees the surfaces of an image that left the tree once it has been off it for
	/// sc_imageSurfaceWrapperReleaseDelayMilliseconds, because freeing them at once made images that come back take several
	/// frames to reload (MUX Reference: corep.h, CCoreServices::TrimMemoryOnSurfaceWrappers in xcpcore.cpp). Tests override
	/// it, as WinUI's do with m_testOverrideImageSurfaceWrapperReleaseDelay.
	/// </summary>
	internal static TimeSpan ReleaseDelay
	{
		get => _releaseDelay;
		set
		{
			_releaseDelay = value;
			StartTimer();
		}
	}

	private static TimeSpan _releaseDelay = TimeSpan.FromMilliseconds(1000);

	// Waits of ReleaseDelay, in the order they started, which is the order they end in, as they all wait the same delay.
	private static readonly LinkedList<Entry> _waits = new();

	// Data released after the layout pass, in the order it was let go, which is also the order of the passes.
	private static readonly LinkedList<Entry> _afterPass = new();

	private static long _pendingBytes;
	private static int _layoutPass;
	private static global::Microsoft.UI.Dispatching.DispatcherQueueTimer? _timer;
	private static long _timerDueAt;
	private static bool _isCheckQueued;

	internal sealed class Entry
	{
		internal Entry(long bytes, Action release)
		{
			Bytes = bytes;
			Release = release;
			Since = Stopwatch.GetTimestamp();
			LayoutPass = _layoutPass;
		}

		internal long Bytes { get; set; }

		internal Action Release { get; }

		internal long Since { get; }

		internal int LayoutPass { get; }

		internal LinkedListNode<Entry>? Node { get; set; }

		internal bool IsPending => Node is not null;
	}

	/// <summary>
	/// Starts the wait of decoded data nothing displays: <see cref="ReleaseDelay"/>, or until after the layout pass
	/// (<paramref name="afterPass"/>), e.g. data that was replaced once its replacement is drawn.
	/// </summary>
	internal static Entry Add(long bytes, Action release, bool afterPass = false)
	{
		var entry = new Entry(bytes, release);
		entry.Node = (afterPass ? _afterPass : _waits).AddLast(entry);
		_pendingBytes += bytes;

		if (afterPass || IsOverLimit)
		{
			RequestCheck();
		}
		else
		{
			StartTimer();
		}

		return entry;
	}

	/// <summary>
	/// Ends the wait without releasing, as the data is displayed again.
	/// </summary>
	internal static void Remove(Entry entry)
	{
		if (entry.Node is { } node)
		{
			node.List!.Remove(node);
			entry.Node = null;
			_pendingBytes -= entry.Bytes;
		}
	}

	/// <summary>
	/// Updates the size of waiting data, e.g. once a decode that was in flight completes.
	/// </summary>
	internal static void Resize(Entry entry, long bytes)
	{
		if (entry.IsPending)
		{
			_pendingBytes += bytes - entry.Bytes;
			entry.Bytes = bytes;

			if (IsOverLimit)
			{
				RequestCheck();
			}
		}
	}

	internal static void OnLayoutPassStarting() => _layoutPass++;

	internal static void OnLayoutPassCompleted()
	{
		if (_afterPass.Count > 0 || IsOverLimit || IsFirstWaitDue)
		{
			ReleaseDue(atLayoutPassEnd: true);
		}
		else
		{
			// Nothing is due yet, e.g. after a timer that fired a little early: the wait goes on.
			StartTimer();
		}
	}

	private static bool IsOverLimit => _pendingBytes > FeatureConfiguration.Image.MaxUndisplayedImageBytes;

	private static bool IsFirstWaitDue => _waits.First is { } first && Stopwatch.GetTimestamp() >= GetDueAt(first.Value);

	private static long GetDueAt(Entry entry)
		=> entry.Since + (long)(ReleaseDelay.TotalSeconds * Stopwatch.Frequency);

	// At the end of a pass, only what was let go before it began: an element re-added during this pass may be loaded by the
	// next one. What was let go during it is at the end of each queue.
	private static bool IsFromThisPass(Entry entry, bool atLayoutPassEnd)
		=> atLayoutPassEnd && entry.LayoutPass == _layoutPass;

	private static void ReleaseDue(bool atLayoutPassEnd)
	{
		if (!atLayoutPassEnd)
		{
			_isCheckQueued = false;
		}

		var waitsForNextPass = false;

		while (_afterPass.First?.Value is { } entry)
		{
			if (IsFromThisPass(entry, atLayoutPassEnd))
			{
				waitsForNextPass = true;
				break;
			}

			Remove(entry);
			entry.Release();
		}

		// The waits end in order; over the limit, the oldest go first. An open still in flight (no bytes yet) is skipped
		// then, as releasing it early would not bring the total under the limit.
		var now = Stopwatch.GetTimestamp();
		for (var node = _waits.First; node is not null;)
		{
			var entry = node.Value;
			var next = node.Next;
			var isOverLimit = IsOverLimit;
			var isDue = now >= GetDueAt(entry) || (isOverLimit && entry.Bytes > 0);

			if (IsFromThisPass(entry, atLayoutPassEnd))
			{
				waitsForNextPass |= isDue || isOverLimit;
				break;
			}

			if (isDue)
			{
				Remove(entry);
				entry.Release();

				// A release can end other waits, the next one included; the walk then starts over.
				if (next is { List: null })
				{
					next = _waits.First;
				}
			}
			else if (!isOverLimit)
			{
				break;
			}

			node = next;
		}

		if (waitsForNextPass)
		{
			RequestCheck();
		}
		else
		{
			StartTimer();
		}
	}

	// Requests a check when the first wait ends, so it ends on an idle UI too.
	private static void StartTimer()
	{
		if (_waits.First?.Value is not { } first)
		{
			return;
		}

		var now = Stopwatch.GetTimestamp();
		var dueAt = Math.Max(GetDueAt(first), now);
		if (_timer is { IsRunning: true } && _timerDueAt <= dueAt)
		{
			return;
		}

		if (_timer is null)
		{
			_timer = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
			_timer.IsRepeating = false;
			_timer.Tick += static (_, _) =>
			{
				if (_waits.Count > 0)
				{
					RequestCheck();
				}
			};
		}

		_timerDueAt = dueAt;
		_timer.Interval = TimeSpan.FromSeconds((dueAt - now) / (double)Stopwatch.Frequency);
		_timer.Start();
	}

	private static void RequestCheck()
	{
		// The check runs at the end of the requested pass, which a busy UI delays but cannot starve. With no window to lay
		// out, nothing can be loaded again before it runs.
		if (CoreServices.HasRootWithBounds())
		{
			CoreServices.RequestAdditionalFrame();
		}
		else if (!_isCheckQueued)
		{
			_isCheckQueued = true;
			NativeDispatcher.Main.Enqueue(static () =>
			{
				// A window that got bounds meanwhile loads what it shows in its next pass, which then runs the check.
				if (CoreServices.HasRootWithBounds())
				{
					_isCheckQueued = false;
					CoreServices.RequestAdditionalFrame();
				}
				else
				{
					ReleaseDue(atLayoutPassEnd: false);
				}
			});
		}
	}
}
