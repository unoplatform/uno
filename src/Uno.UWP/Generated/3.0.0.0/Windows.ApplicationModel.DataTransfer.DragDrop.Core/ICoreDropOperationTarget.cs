#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICoreDropOperationTarget 
	{
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackageOperation> EnterAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo,  global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragUIOverride dragUIOverride);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackageOperation> OverAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo,  global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragUIOverride dragUIOverride);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction LeaveAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackageOperation> DropAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo);
		#endif
	}
}
