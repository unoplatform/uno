#nullable enable

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml
{
	public partial class DragEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly PointerRoutedEventArgs _pointer;

		internal DragEventArgs(UIElement originalSource, PointerRoutedEventArgs pointer)
			: base(originalSource)
		{
			_pointer = pointer;
		}

		public bool Handled { get; set; }

		public DataPackageOperation AllowedOperations { get; }
		public DataPackageOperation AcceptedOperation { get; set; }

		public DataPackage Data { get; set; }
		public DataPackageView DataView { get; }
		public DragUIOverride DragUIOverride { get; } // TODO, is this required for the initial D&D support?

		public DragDropModifiers Modifiers { get; }

		public Point GetPosition(UIElement relativeTo)
			=> _pointer.GetCurrentPoint(relativeTo).Position;

		//public  global::Windows.UI.Xaml.DragOperationDeferral GetDeferral() // TODO, is this required for the initial D&D support?
	}
}
