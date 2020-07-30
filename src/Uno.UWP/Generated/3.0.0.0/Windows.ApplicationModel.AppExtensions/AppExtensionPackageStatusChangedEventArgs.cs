#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppExtensions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppExtensionPackageStatusChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AppExtensionName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppExtensionPackageStatusChangedEventArgs.AppExtensionName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package AppExtensionPackageStatusChangedEventArgs.Package is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionPackageStatusChangedEventArgs.AppExtensionName.get
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionPackageStatusChangedEventArgs.Package.get
	}
}
