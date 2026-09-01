#nullable enable

using System;
using Microsoft.UI.Composition;
using Uno.Foundation.Logging;
using System.Linq;
using Uno.UI.Composition;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private readonly FrameClock _frameClock = new();

	private EventHandler<long>? _frameStarting;
	private bool _frameDriverTickArmed;

	/// <summary>
	/// Raised once per frame, from the tick that precedes it (before layout and before the record), with
	/// the timestamp every driver in that frame must evaluate against.
	/// </summary>
	/// <remarks>
	/// Deliberately not raised from inside the record. A driver writing there produces a frame request
	/// that the render state machine cannot tell apart from "content changed since the last record", so
	/// it promises a picture that tick will not produce and the previous one is shown again. Writing
	/// before layout makes a driver's write an ordinary pre-frame invalidation — the same shape as a
	/// pointer event's — and lets the same tick clean up the layout it dirties.
	/// </remarks>
	internal event EventHandler<long>? FrameStarting
	{
		add
		{
			var wasEmpty = _frameStarting is null;
			_frameStarting += value;
			if (wasEmpty && _frameStarting is not null)
			{
				Compositor.AddFrameDriver();

				// A tick, not a frame: the driver is raised from the tick, and requesting only a render
				// would leave a newly subscribed driver waiting for one that may never come.
				ArmFrameDriverTick();
			}
		}
		remove
		{
			var wasPresent = _frameStarting is not null;
			_frameStarting -= value;
			if (wasPresent && _frameStarting is null)
			{
				Compositor.RemoveFrameDriver();

				// The grid's phase means nothing across the gap until the next motion starts.
				_frameClock.Reset();
			}
		}
	}

	internal bool HasFrameDrivers => _frameStarting is not null;

	/// <summary>The target for frame drivers that have no visual of their own.</summary>
	/// <remarks>
	/// TODO Uno: thread the owning element's visual through the gesture recognizer so a multi-window app
	/// ticks such a driver against its own window's target rather than the first one.
	/// </remarks>
	internal static CompositionTarget? MainFrameDriverTarget
		=> global::Uno.UI.ApplicationHelper.WindowsInternal.FirstOrDefault()?.RootElement?.XamlRoot?.Content?.Visual.CompositionTarget as CompositionTarget;

	/// <summary>The timestamp this frame's drivers were evaluated against.</summary>
	internal long CurrentFrameTimestampInTicks { get; private set; }

	/// <summary>Estimated interval between presented frames, for drivers that need a nominal step.</summary>
	internal long FrameIntervalInTicks => _frameClock.IntervalInTicks;

	/// <summary>
	/// Arms the next tick to evaluate the frame drivers. Called once per native frame, from the frame's
	/// UI-thread callback, so drivers step on the presented cadence rather than on every dispatcher pump.
	/// </summary>
	/// <remarks>
	/// The tick is otherwise on demand and re-requests itself while there is layout to do, so ticking the
	/// drivers from every tick would free-run them between two vsyncs: each pass samples wall-clock jitter
	/// into the motion, runs a layout pass of its own, and collapses the frame clock's interval so its grid
	/// never engages.
	/// </remarks>
	internal void ArmFrameDriverTick()
	{
		if (_frameStarting is null)
		{
			return;
		}

		_frameDriverTickArmed = true;
		CoreServices.RequestAdditionalFrame();
	}

	/// <summary>Ticks every frame driver for this target. Called from the tick, before layout.</summary>
	internal void RaiseFrameStarting()
	{
		if (!_frameDriverTickArmed || _frameStarting is not { } frameStarting)
		{
			return;
		}

		_frameDriverTickArmed = false;

		var timestamp = _frameClock.NextTimestamp(Compositor.GetSharedCompositor().TimestampInTicks);
		CurrentFrameTimestampInTicks = timestamp;

		try
		{
			frameStarting(this, timestamp);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("A frame driver threw; the frame is still recorded.", e);
			}
		}

		// The chain that brings the next tick is the frame itself: a driver that wrote nothing this frame
		// (or is about to stop) must still get its next tick, so request the frame on its behalf.
		if (_frameStarting is not null)
		{
			((ICompositionTarget)this).RequestNewFrame();
		}
	}
}
