#nullable enable

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;

namespace Windows.UI.Xaml
{
	public partial class DropCompletedEventArgs : RoutedEventArgs
	{
		internal DropCompletedEventArgs(UIElement originalSource, CoreDragInfo info, DataPackageOperation result)
			: base(originalSource)
		{
			Info = info;
			DropResult = result;

			CanBubbleNatively = false;
		}

		/// <summary>
		/// Get access to the shared data, for internal usage only.
		/// This has to be treated as readonly for drop completed.
		/// </summary>
		internal CoreDragInfo Info { get; }

		public DataPackageOperation DropResult { get; }
	}
}
