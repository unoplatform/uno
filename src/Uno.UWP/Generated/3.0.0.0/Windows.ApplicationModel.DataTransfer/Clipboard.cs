#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if __ANDROID__ || __IOS__ || NET46 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Clipboard 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.ApplicationModel.DataTransfer.DataPackageView GetContent()
		{
			throw new global::System.NotImplementedException("The member DataPackageView Clipboard.GetContent() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetContent( global::Windows.ApplicationModel.DataTransfer.DataPackage content)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.SetContent(DataPackage content)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void Flush()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.Flush()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void Clear()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.Clear()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> ContentChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.ContentChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.ContentChanged");
			}
		}
		#endif
	}
}
