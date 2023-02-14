#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RedirectVisual : global::Microsoft.UI.Composition.ContainerVisual
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Composition.Visual Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member Visual RedirectVisual.Source is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Visual%20RedirectVisual.Source");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Composition.RedirectVisual", "Visual RedirectVisual.Source");
			}
		}
		#endif
		// Forced skipping of method Microsoft.UI.Composition.RedirectVisual.Source.get
		// Forced skipping of method Microsoft.UI.Composition.RedirectVisual.Source.set
	}
}
