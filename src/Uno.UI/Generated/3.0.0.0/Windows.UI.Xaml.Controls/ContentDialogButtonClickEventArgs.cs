#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentDialogButtonClickEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ContentDialogButtonClickEventArgs.Cancel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs", "bool ContentDialogButtonClickEventArgs.Cancel");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs.Cancel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs.Cancel.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member ContentDialogButtonClickDeferral ContentDialogButtonClickEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
