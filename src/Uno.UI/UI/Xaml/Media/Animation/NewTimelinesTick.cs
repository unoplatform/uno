#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

internal interface INewTimelineTick
{
	bool IsAwaitingNewTimelineTick { get; }

	void TickNewTimeline();
}

/// <summary>
/// Stand-in for WinUI's post-layout <c>CTimeManager::Tick(newTimelinesOnly: true)</c>: animations begun since the last
/// frame get their starting value applied before the frame is recorded, instead of on the next Rendering tick.
/// </summary>
internal static class NewTimelinesTick
{
	private const int MinCompactThreshold = 64;

	private static readonly List<INewTimelineTick> _pending = new();
	private static int _compactThreshold = MinCompactThreshold;

	internal static void Register(INewTimelineTick timeline)
	{
		// No frame is recorded while a window is hidden, but animations keep restarting.
		if (_pending.Count >= _compactThreshold)
		{
			_pending.RemoveAll(static t => !t.IsAwaitingNewTimelineTick);
			_compactThreshold = Math.Max(MinCompactThreshold, _pending.Count * 2);
		}

		_pending.Add(timeline);
	}

	internal static void Run()
	{
		try
		{
			// Ticking can begin further timelines (keyframe segments, deferred Begin), which are ticked in the same pass.
			for (var i = 0; i < _pending.Count; i++)
			{
				_pending[i].TickNewTimeline();
			}
		}
		finally
		{
			_pending.Clear();
			_compactThreshold = MinCompactThreshold;
		}
	}
}
