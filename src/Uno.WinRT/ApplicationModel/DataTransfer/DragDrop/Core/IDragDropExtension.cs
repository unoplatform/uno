using System;
using System.Linq;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal interface IDragDropExtension
	{
		/// <summary>
		/// Whether <see cref="StartNativeDrag"/> is called as soon as a drag starts rather than once the pointer leaves
		/// the window. Needed where only the platform knows which window is under the pointer: a Wayland compositor
		/// keeps the implicit grab on the origin surface, so a drag over a covering window never leaves it.
		/// </summary>
		bool StartsNativeDragImmediately => false;

		/// <summary>
		/// Hands the drag to the platform, which then shows the drag visual and reports the operation the target
		/// performed through <paramref name="onCompleted"/>. False when the platform cannot take it, in which case
		/// the managed drag continues.
		/// </summary>
		bool StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> onCompleted);

		/// <summary>
		/// Raised when the operation the target accepts becomes known, which is after the Enter/Over handlers
		/// complete, so possibly later than the move that triggered them (deferrals).
		/// </summary>
		void AcceptedOperationChanged(CoreDragInfo info, DataPackageOperation accepted)
		{
		}
	}
}
