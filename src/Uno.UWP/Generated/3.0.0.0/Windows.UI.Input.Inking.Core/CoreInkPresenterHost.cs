#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreInkPresenterHost 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.ContainerVisual RootVisual
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContainerVisual CoreInkPresenterHost.RootVisual is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.Core.CoreInkPresenterHost", "ContainerVisual CoreInkPresenterHost.RootVisual");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkPresenter InkPresenter
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkPresenter CoreInkPresenterHost.InkPresenter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CoreInkPresenterHost() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.Core.CoreInkPresenterHost", "CoreInkPresenterHost.CoreInkPresenterHost()");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.Core.CoreInkPresenterHost.CoreInkPresenterHost()
		// Forced skipping of method Windows.UI.Input.Inking.Core.CoreInkPresenterHost.InkPresenter.get
		// Forced skipping of method Windows.UI.Input.Inking.Core.CoreInkPresenterHost.RootVisual.get
		// Forced skipping of method Windows.UI.Input.Inking.Core.CoreInkPresenterHost.RootVisual.set
	}
}
