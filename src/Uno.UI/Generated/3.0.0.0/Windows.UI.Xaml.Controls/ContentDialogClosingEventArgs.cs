#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentDialogClosingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ContentDialogClosingEventArgs.Cancel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs", "bool ContentDialogClosingEventArgs.Cancel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ContentDialogResult Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContentDialogResult ContentDialogClosingEventArgs.Result is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs.Result.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs.Cancel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs.Cancel.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ContentDialogClosingDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member ContentDialogClosingDeferral ContentDialogClosingEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
