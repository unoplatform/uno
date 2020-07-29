#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICoreDropOperationTarget 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackageOperation> EnterAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo,  global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragUIOverride dragUIOverride);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackageOperation> OverAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo,  global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragUIOverride dragUIOverride);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncAction LeaveAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackageOperation> DropAsync( global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragInfo dragInfo);
		#endif
	}
}
