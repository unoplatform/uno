#nullable enable

using System;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland;

internal interface IWaylandClipboardDevice
{
	event EventHandler? SelectionChanged;
	bool HasSelection { get; }
	void FillContent(DataPackage package);
	void Post(Action action);
	void RequestSelection(Action publish, Action? cancel = null);
	void CancelPendingSelection();
	WlDataSource CreateSource();
	void SetSelection(WlDataSource? source);
}
