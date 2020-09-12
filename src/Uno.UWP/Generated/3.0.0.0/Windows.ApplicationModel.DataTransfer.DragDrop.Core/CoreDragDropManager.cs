#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreDragDropManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AreConcurrentOperationsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreDragDropManager.AreConcurrentOperationsEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager", "bool CoreDragDropManager.AreConcurrentOperationsEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager.TargetRequested.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager.TargetRequested.remove
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager.AreConcurrentOperationsEnabled.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager.AreConcurrentOperationsEnabled.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member CoreDragDropManager CoreDragDropManager.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager, global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDropOperationTargetRequestedEventArgs> TargetRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager", "event TypedEventHandler<CoreDragDropManager, CoreDropOperationTargetRequestedEventArgs> CoreDragDropManager.TargetRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager", "event TypedEventHandler<CoreDragDropManager, CoreDropOperationTargetRequestedEventArgs> CoreDragDropManager.TargetRequested");
			}
		}
		#endif
	}
}
