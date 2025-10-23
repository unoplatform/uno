#nullable enable

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Uno.UI;

namespace Microsoft.UI.Xaml
{
	public partial class DragStartingEventArgs : RoutedEventArgs
	{
		private readonly PointerRoutedEventArgs _pointer;

		internal DragStartingEventArgs(UIElement originalSource, PointerRoutedEventArgs pointer)
			: base(originalSource)
		{
			_pointer = pointer;

			CanBubbleNatively = false;

			DragUI = new DragUI(_pointer.Pointer.PointerDeviceType);
		}

		public bool Cancel { get; set; }

		public DataPackage Data { get; } = new DataPackage();

		public DragUI DragUI { get; }

		public DataPackageOperation AllowedOperations { get; set; } = DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link;

		internal DragOperationDeferral? Deferral { get; private set; }

		public Point GetPosition(UIElement relativeTo)
			=> _pointer.GetCurrentPoint(relativeTo).Position;

		public DragOperationDeferral GetDeferral()
			=> Deferral ??= new DragOperationDeferral();
	}
}
