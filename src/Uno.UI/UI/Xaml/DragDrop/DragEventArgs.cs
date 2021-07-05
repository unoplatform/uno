#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml
{
	public partial class DragEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly CoreDragInfo _info;

		internal DragEventArgs(
			UIElement originalSource,
			CoreDragInfo info,
			DragUIOverride uiOverride)
			: base(originalSource)
		{
			CanBubbleNatively = false;

			_info = info;

			DragUIOverride = uiOverride;
		}

		#region Readonly properties that gives context to the handler
		public bool Handled { get; set; }

		public DataPackage Data { get; set; } = new DataPackage(); // This is actually not used

		public DataPackageView DataView => _info.Data;

		public DataPackageOperation AllowedOperations => _info.AllowedOperations;

		public DragDropModifiers Modifiers => _info.Modifiers;

		internal long SourceId => _info.SourceId;
		#endregion

		#region Properties that are expected to be updated by the handler
		public DataPackageOperation AcceptedOperation { get; set; }

		public DragUIOverride DragUIOverride { get; }

		internal DragOperationDeferral? Deferral { get; private set; }
		#endregion

		public Point GetPosition(UIElement relativeTo)
			=> _info.GetPosition(relativeTo);

		public DragOperationDeferral GetDeferral()
			=> Deferral ??= new DragOperationDeferral();
	}
}
