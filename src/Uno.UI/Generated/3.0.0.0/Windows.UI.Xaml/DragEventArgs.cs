#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DragEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DragEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DragEventArgs", "bool DragEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackage Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackage DragEventArgs.Data is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DragEventArgs", "DataPackage DragEventArgs.Data");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageOperation AcceptedOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageOperation DragEventArgs.AcceptedOperation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.DragEventArgs", "DataPackageOperation DragEventArgs.AcceptedOperation");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageView DataView
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageView DragEventArgs.DataView is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.DragUIOverride DragUIOverride
		{
			get
			{
				throw new global::System.NotImplementedException("The member DragUIOverride DragEventArgs.DragUIOverride is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DragDrop.DragDropModifiers Modifiers
		{
			get
			{
				throw new global::System.NotImplementedException("The member DragDropModifiers DragEventArgs.Modifiers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageOperation AllowedOperations
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageOperation DragEventArgs.AllowedOperations is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.Data.get
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.Data.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point GetPosition( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member Point DragEventArgs.GetPosition(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.DataView.get
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.DragUIOverride.get
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.Modifiers.get
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.AcceptedOperation.get
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.AcceptedOperation.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.DragOperationDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member DragOperationDeferral DragEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DragEventArgs.AllowedOperations.get
	}
}
