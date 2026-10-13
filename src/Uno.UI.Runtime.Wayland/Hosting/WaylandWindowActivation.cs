#nullable enable

using System;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland;

internal sealed class WaylandWindowActivation(Action<Action> postToUI, Func<bool> canNotify, Action<CoreWindowActivationState> notify)
{
	private bool _pending;
	private WaylandToplevelStates _states;

	internal bool HasAppliedConfigure { get; private set; }

	// Loaded can run inside CoreServices' window enumeration. The host delivers activation on a UI turn.
	internal bool IsReadyForActivation => false;

	internal void Request()
	{
		_pending = true;
		postToUI(() =>
		{
			if (HasAppliedConfigure && _pending)
			{
				Raise();
			}
		});
	}

	internal void Configure(WaylandToplevelStates states, bool notifyActivation)
	{
		HasAppliedConfigure = true;
		_states = states;
		if (notifyActivation)
		{
			Raise();
		}
	}

	private void Raise()
	{
		var initialActivation = _pending;
		_pending = false;
		foreach (var state in WaylandToplevelStateParser.GetActivationSequence(_states, initialActivation))
		{
			if (!canNotify())
			{
				break;
			}
			notify(state);
		}
	}
}
