#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreTextLayoutBounds 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect TextBounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect CoreTextLayoutBounds.TextBounds is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.Core.CoreTextLayoutBounds", "Rect CoreTextLayoutBounds.TextBounds");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect ControlBounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect CoreTextLayoutBounds.ControlBounds is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.Core.CoreTextLayoutBounds", "Rect CoreTextLayoutBounds.ControlBounds");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.Core.CoreTextLayoutBounds.TextBounds.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextLayoutBounds.TextBounds.set
		// Forced skipping of method Windows.UI.Text.Core.CoreTextLayoutBounds.ControlBounds.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextLayoutBounds.ControlBounds.set
	}
}
