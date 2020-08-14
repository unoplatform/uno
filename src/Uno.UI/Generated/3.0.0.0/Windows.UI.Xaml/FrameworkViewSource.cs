#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FrameworkViewSource : global::Windows.ApplicationModel.Core.IFrameworkViewSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FrameworkViewSource() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.FrameworkViewSource", "FrameworkViewSource.FrameworkViewSource()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.FrameworkViewSource.FrameworkViewSource()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Core.IFrameworkView CreateView()
		{
			throw new global::System.NotImplementedException("The member IFrameworkView FrameworkViewSource.CreateView() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.ApplicationModel.Core.IFrameworkViewSource
	}
}
