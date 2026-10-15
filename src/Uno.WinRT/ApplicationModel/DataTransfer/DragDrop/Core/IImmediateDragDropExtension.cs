#nullable enable

using System;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	/// <summary>
	/// A drag-drop extension that can hand a drag to the platform as soon as it starts, rather than once the
	/// pointer leaves the window. Required where only the platform knows which window is under the pointer
	/// (a Wayland compositor keeps the implicit grab on the origin surface, so overlapping windows never "leave").
	/// </summary>
	internal interface IImmediateDragDropExtension : IDragDropExtension
	{
		/// <summary>
		/// Starts the platform drag for <paramref name="info"/>. Returns false when the platform cannot take it
		/// (nothing to offer, no input grab), in which case the managed drag continues as usual.
		/// </summary>
		bool TryStartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted);
	}
}
