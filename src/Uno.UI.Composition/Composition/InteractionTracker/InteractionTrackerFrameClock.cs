#nullable enable

using System;

namespace Microsoft.UI.Composition.Interactions;

/// <summary>
/// Per-frame tick, raised on the UI thread, that drives InteractionTracker custom animations.
/// Uno.UI points it at CompositionTarget.Rendering.
/// </summary>
internal static class InteractionTrackerFrameClock
{
	internal static Action<EventHandler<object>>? AddFrameHandler { get; set; }

	internal static Action<EventHandler<object>>? RemoveFrameHandler { get; set; }

	internal static bool TrySubscribe(EventHandler<object> handler)
	{
		if (AddFrameHandler is not { } add)
		{
			return false;
		}

		add(handler);
		return true;
	}

	internal static void Unsubscribe(EventHandler<object> handler) => RemoveFrameHandler?.Invoke(handler);
}
