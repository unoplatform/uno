#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionColorGradientStop : global::Windows.UI.Composition.CompositionObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Offset
		{
			get
			{
				throw new global::System.NotImplementedException("The member float CompositionColorGradientStop.Offset is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionColorGradientStop", "float CompositionColorGradientStop.Offset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Color Color
		{
			get
			{
				throw new global::System.NotImplementedException("The member Color CompositionColorGradientStop.Color is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionColorGradientStop", "Color CompositionColorGradientStop.Color");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionColorGradientStop.Color.get
		// Forced skipping of method Windows.UI.Composition.CompositionColorGradientStop.Color.set
		// Forced skipping of method Windows.UI.Composition.CompositionColorGradientStop.Offset.get
		// Forced skipping of method Windows.UI.Composition.CompositionColorGradientStop.Offset.set
	}
}
