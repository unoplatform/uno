#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Composition;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private readonly FrameClock _frameClock = new();

	private EventHandler<long>? _frameStarting;
	private bool _frameTickArmed;

	// Sampled once per native frame; everything that frame evaluates (drivers, Rendering, the record) uses it.
	private long _frameTimestamp;
	private bool _isFrameTimestampFresh;

	private static long _lastRenderingTimestamp;
	private static bool _isAnyFrameTickArmed;

	/// <summary>
	/// Raised once per frame from the tick that precedes the record (before layout), with the timestamp every
	/// driver in that frame must evaluate against.
	/// </summary>
	/// <remarks>
	/// Not raised from inside the record: a write there is a frame request the render state machine cannot tell
	/// apart from "content changed since the last record". Writing before layout makes it an ordinary pre-frame
	/// invalidation, and lets the same tick clean up the layout it dirties.
	/// </remarks>
	internal event EventHandler<long>? FrameStarting
	{
		add
		{
			if (value is null)
			{
				return;
			}

			var wasEmpty = _frameStarting is null;
			_frameStarting += value;
			if (wasEmpty)
			{
				Compositor.AddFrameDriver();

				// The next frame arms its tick. Arming one right away would tick every driver a second time within
				// the frame whenever one is swapped for another, as the InteractionTracker does on every wheel notch.
				((ICompositionTarget)this).RequestNewFrame();
			}
		}
		remove
		{
			if (value is null)
			{
				return;
			}

			var wasPresent = _frameStarting is not null;
			_frameStarting -= value;
			if (wasPresent && _frameStarting is null)
			{
				Compositor.RemoveFrameDriver();
			}
		}
	}

	event EventHandler<long>? ICompositionTarget.FrameStarting
	{
		add => FrameStarting += value;
		remove => FrameStarting -= value;
	}

	long ICompositionTarget.FrameIntervalInTicks => FrameIntervalInTicks;

	/// <summary>Estimated interval between presented frames, for drivers that need a nominal step.</summary>
	internal long FrameIntervalInTicks => _frameClock.IntervalInTicks;

	/// <summary>The target for frame drivers with no visual of their own, such as a free-standing InteractionTracker.</summary>
	/// <remarks>Falls back to the primary XamlRoot so an island host, which has no Window, still resolves one.</remarks>
	private static CompositionTarget? MainFrameDriverTarget
		=> (global::Uno.UI.ApplicationHelper.WindowsInternal.FirstOrDefault()?.RootElement?.XamlRoot
			?? CoreServices.GetXamlRoot())?.VisualTree.ContentRoot.CompositionTarget;

	private bool HasFrameTickWork => _frameStarting is not null || _isRenderingActive;

	private void SampleFrameTimestamp()
	{
		_frameTimestamp = _frameClock.NextTimestamp(Compositor.GetSharedCompositor().TimestampInTicks);
		_isFrameTimestampFresh = true;
	}

	private void ArmFrameTick()
	{
		if (!HasFrameTickWork)
		{
			// No tick will use this frame's timestamp, and one armed later (a driver starting after a pause) would
			// otherwise take it as current and date its motion from however long ago this frame was.
			_isFrameTimestampFresh = false;
			return;
		}

		_frameTickArmed = true;
		_isAnyFrameTickArmed = true;
		CoreServices.RequestAdditionalFrame();
	}

	/// <summary>
	/// Drops the frame drivers of a target whose host is gone. It never presents again, so its drivers would
	/// never tick again either, and <see cref="Compositor.IsAnimating"/> would report them forever.
	/// </summary>
	internal void ClearFrameDrivers()
	{
		_frameTickArmed = false;

		if (_frameStarting is null)
		{
			return;
		}

		_frameStarting = null;
		Compositor.RemoveFrameDriver();
		_frameClock.Reset();
	}

	/// <summary>
	/// Raises the per-frame work of every armed target: its frame drivers, then <see cref="Rendering"/> once for
	/// all of them. Called from the tick, before layout and before the record.
	/// </summary>
	internal static void RaiseFrameTick()
	{
		// Most ticks are for layout alone, and walking the targets allocates an enumerator.
		if (!_isAnyFrameTickArmed)
		{
			return;
		}

		_isAnyFrameTickArmed = false;

		long? renderingTimestamp = null;

		foreach (var (target, _) in _targets)
		{
			if (target._frameTickArmed && target.RaiseFrameStarting() is { } timestamp)
			{
				renderingTimestamp ??= timestamp;
			}
		}

		if (renderingTimestamp is { } frameTimestamp && _isRenderingActive)
		{
			// Several windows can arm the same tick, and their clocks are not in phase.
			_lastRenderingTimestamp = Math.Max(_lastRenderingTimestamp, frameTimestamp);
			InvokeRendering(_lastRenderingTimestamp);
		}
	}

	private long? RaiseFrameStarting()
	{
		_frameTickArmed = false;

		if (!HasFrameTickWork)
		{
			return null;
		}

		// A tick armed by a new driver rather than by a frame: the last sample may be stale by an idle gap.
		if (!_isFrameTimestampFresh)
		{
			SampleFrameTimestamp();
		}

		_isFrameTimestampFresh = false;
		var timestamp = _frameTimestamp;

		if (_frameStarting is { } frameStarting)
		{
			// One try/catch per driver: a multicast invocation stops at the first handler that throws.
			foreach (var handler in Delegate.EnumerateInvocationList(frameStarting))
			{
				try
				{
					handler(this, timestamp);
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("A frame driver threw; the frame is still recorded.", e);
					}
				}
			}

			// The next tick comes from the next frame, so a driver that wrote nothing still needs one.
			if (_frameStarting is not null)
			{
				((ICompositionTarget)this).RequestNewFrame();
			}
		}

		return timestamp;
	}
}
