#nullable enable

namespace Uno.UI.Runtime.Wayland;

// Guarded by the display's SyncRoot, including action events from the compositor.
internal sealed class WaylandDropNegotiation
{
	internal uint SelectedAction { get; set; }
	internal bool AcceptedMime { get; set; }
	internal uint AcceptedActions { get; set; }
	internal bool Dropped { get; set; }

	internal bool CanDrop => AcceptedMime && SelectedAction is 1 or 2 && (AcceptedActions & SelectedAction) != 0;

	internal bool CanFinish(uint completedAction) => Dropped && CanDrop && completedAction == SelectedAction;

	// The single wl_data_device_manager.dnd_action to offer: copy when possible, never one the source lacks.
	internal static uint ChooseAction(uint acceptedActions, uint sourceActions)
		=> (acceptedActions & sourceActions & 1) != 0 ? 1u : acceptedActions & sourceActions & 2;
}
