#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PalmRejectionDelayZonePreview : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.Preview.PalmRejectionDelayZonePreview", "void PalmRejectionDelayZonePreview.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.Preview.PalmRejectionDelayZonePreview.CreateForVisual(Windows.UI.Composition.Visual, Windows.Foundation.Rect)
		// Forced skipping of method Windows.UI.Input.Inking.Preview.PalmRejectionDelayZonePreview.CreateForVisual(Windows.UI.Composition.Visual, Windows.Foundation.Rect, Windows.UI.Composition.Visual, Windows.Foundation.Rect)
		// Processing: System.IDisposable
	}
}
