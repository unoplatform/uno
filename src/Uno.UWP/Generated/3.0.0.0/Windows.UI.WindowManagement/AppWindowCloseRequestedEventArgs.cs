#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppWindowCloseRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppWindowCloseRequestedEventArgs.Cancel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.AppWindowCloseRequestedEventArgs", "bool AppWindowCloseRequestedEventArgs.Cancel");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.AppWindowCloseRequestedEventArgs.Cancel.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindowCloseRequestedEventArgs.Cancel.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral AppWindowCloseRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
