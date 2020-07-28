#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreDragOperation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragUIContentMode DragUIContentMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreDragUIContentMode CoreDragOperation.DragUIContentMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation", "CoreDragUIContentMode CoreDragOperation.DragUIContentMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackage Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackage CoreDragOperation.Data is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageOperation AllowedOperations
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageOperation CoreDragOperation.AllowedOperations is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation", "DataPackageOperation CoreDragOperation.AllowedOperations");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CoreDragOperation() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation", "CoreDragOperation.CoreDragOperation()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation.CoreDragOperation()
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation.Data.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPointerId( uint pointerId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation", "void CoreDragOperation.SetPointerId(uint pointerId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDragUIContentFromSoftwareBitmap( global::Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation", "void CoreDragOperation.SetDragUIContentFromSoftwareBitmap(SoftwareBitmap softwareBitmap)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDragUIContentFromSoftwareBitmap( global::Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap,  global::Windows.Foundation.Point anchorPoint)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation", "void CoreDragOperation.SetDragUIContentFromSoftwareBitmap(SoftwareBitmap softwareBitmap, Point anchorPoint)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation.DragUIContentMode.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation.DragUIContentMode.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.DataPackageOperation> StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DataPackageOperation> CoreDragOperation.StartAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation.AllowedOperations.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragOperation.AllowedOperations.set
	}
}
