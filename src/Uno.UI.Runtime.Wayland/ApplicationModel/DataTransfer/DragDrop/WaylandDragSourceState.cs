#nullable enable

namespace Uno.UI.Runtime.Wayland;

internal enum WaylandDragSourcePhase
{
	Dragging,
	Dropped,
	Finished,
	Cancelled,
}

// The wl_data_source side of one outgoing drag, fed with its events on the dispatch thread. The result is the
// wl_data_device_manager.dnd_action the target performed, or 0 when the drag was cancelled or rejected.
internal sealed class WaylandDragSourceState
{
	// wl_data_device_manager.dnd_action
	internal const uint ActionCopy = 1;
	internal const uint ActionMove = 2;

	internal WaylandDragSourcePhase Phase { get; private set; } = WaylandDragSourcePhase.Dragging;
	// Whether the last wl_data_source.target carried a MIME type.
	internal bool TargetAccepts { get; private set; }
	// The action of the last wl_data_source.action event; the compositor may change it until dnd_finished.
	internal uint Action { get; private set; }
	internal bool DataSent { get; private set; }
	internal uint Result { get; private set; }

	internal bool IsComplete => Phase is WaylandDragSourcePhase.Finished or WaylandDragSourcePhase.Cancelled;

	internal void Target(string? mimeType)
	{
		if (!IsComplete)
		{
			TargetAccepts = mimeType is not null;
		}
	}

	internal void SetAction(uint dndAction)
	{
		if (!IsComplete)
		{
			Action = dndAction & (ActionCopy | ActionMove);
		}
	}

	internal void Sent()
	{
		if (!IsComplete)
		{
			DataSent = true;
		}
	}

	internal void DropPerformed()
	{
		if (Phase == WaylandDragSourcePhase.Dragging)
		{
			Phase = WaylandDragSourcePhase.Dropped;
		}
	}

	/// <summary>wl_data_source.dnd_finished: the target performed the current action. Returns false when already complete.</summary>
	internal bool Finished()
	{
		if (IsComplete)
		{
			return false;
		}
		Phase = WaylandDragSourcePhase.Finished;
		// A target that read the data without ever negotiating an action (pre-v3 offers) copied it.
		Result = Action != 0 ? Action : DataSent ? ActionCopy : 0;
		return true;
	}

	/// <summary>wl_data_source.cancelled: rejected, escaped, or dropped on a target that never finished. Returns false when already complete.</summary>
	internal bool Cancelled()
	{
		if (IsComplete)
		{
			return false;
		}
		Phase = WaylandDragSourcePhase.Cancelled;
		Result = 0;
		return true;
	}
}
