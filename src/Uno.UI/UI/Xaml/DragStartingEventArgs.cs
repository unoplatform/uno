#nullable  enable

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class DragStartingEventArgs : RoutedEventArgs
	{
		private readonly PointerRoutedEventArgs _pointer;
		private DragOperationDeferral? _deferral;

		internal DragStartingEventArgs(UIElement originalSource, PointerRoutedEventArgs pointer)
			: base(originalSource)
		{
			_pointer = pointer;
		}

		public bool Cancel { get; set; }

		public DataPackage Data { get; } = new DataPackage();

		public DragUI DragUI { get; } = new DragUI();

		public DataPackageOperation AllowedOperations { get; set; } = DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link;

		public Point GetPosition(UIElement relativeTo)
			=> _pointer.GetCurrentPoint(relativeTo).Position;

		public DragOperationDeferral GetDeferral()
			=> _deferral ??= new DragOperationDeferral(this);
	}
}
