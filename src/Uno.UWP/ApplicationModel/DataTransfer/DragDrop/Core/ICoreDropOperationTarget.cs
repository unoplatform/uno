#nullable enable

using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial interface ICoreDropOperationTarget 
	{
		IAsyncOperation<DataPackageOperation> EnterAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride);
		IAsyncOperation<DataPackageOperation> OverAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride);
		IAsyncAction LeaveAsync( CoreDragInfo dragInfo);
		IAsyncOperation<DataPackageOperation> DropAsync(CoreDragInfo dragInfo);
	}
}
