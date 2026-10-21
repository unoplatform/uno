#nullable enable

using System;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland;

internal interface IWaylandClipboardDevice
{
	event EventHandler? SelectionChanged;
	// Wayland only accepts a selection from the client with keyboard focus.
	bool HasKeyboardFocus { get; }
	void FillContent(DataPackage package);
	void Post(Action action);
	WlDataSource CreateSource();
	void SetSelection(WlDataSource? source);
}
