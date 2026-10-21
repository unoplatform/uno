#nullable enable

using System;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland;

// UI thread. The window implementation raises CodeActivated once Show returns; configures report focus changes.
internal sealed class WaylandWindowActivation(Action<Action> postToUI, Func<bool> canNotify, Action<CoreWindowActivationState> notify)
{
	private int _generation;

	internal bool HasAppliedConfigure { get; private set; }

	internal void Configure(WaylandToplevelStates states, bool notifyActivation)
	{
		var initial = !HasAppliedConfigure;
		HasAppliedConfigure = true;
		if (!notifyActivation)
		{
			return;
		}
		var focused = (states & WaylandToplevelStates.Activated) != 0;
		if (initial)
		{
			// The first configure is applied while the window is shown, before its CodeActivated: only focus follows it.
			if (focused)
			{
				var generation = _generation;
				postToUI(() =>
				{
					// A later configure already reported focus; do not override it.
					if (generation == _generation)
					{
						Notify(CoreWindowActivationState.PointerActivated);
					}
				});
			}
			return;
		}
		_generation++;
		Notify(focused ? CoreWindowActivationState.PointerActivated : CoreWindowActivationState.Deactivated);
	}

	private void Notify(CoreWindowActivationState state)
	{
		if (canNotify())
		{
			notify(state);
		}
	}
}
