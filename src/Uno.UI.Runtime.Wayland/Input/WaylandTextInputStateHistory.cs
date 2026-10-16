#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Runtime.Wayland;

// The caller holds the display lock. Compositors need not acknowledge every committed state.
internal sealed class WaylandTextInputStateHistory<THost>(int capacity) where THost : class
{
	internal readonly record struct State(long Session, THost Host, WaylandTextInputText.SurroundingText? Surrounding, string Text, long Revision);

	private readonly Dictionary<uint, State> _states = new();
	private readonly Queue<uint> _order = new();

	internal void Record(uint serial, State state)
	{
		if (!_states.ContainsKey(serial))
		{
			_order.Enqueue(serial);
		}
		_states[serial] = state;
		while (_order.Count > capacity)
		{
			_states.Remove(_order.Dequeue());
		}
	}

	// Missing snapshots prevent deletion, not delivery of the session's commit and preedit.
	internal State GetStateOrCurrent(uint serial, long session, THost host)
		=> _states.TryGetValue(serial, out var state) ? state : new(session, host, null, string.Empty, -1);

	internal bool TryGetValue(uint serial, out State state) => _states.TryGetValue(serial, out state);

	internal void Clear()
	{
		_states.Clear();
		_order.Clear();
	}

	internal void RetainOnly(uint serial)
	{
		var found = _states.TryGetValue(serial, out var state);
		Clear();
		if (found)
		{
			Record(serial, state);
		}
	}
}
