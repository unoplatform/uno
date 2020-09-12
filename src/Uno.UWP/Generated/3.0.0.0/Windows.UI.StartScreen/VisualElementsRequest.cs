#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.StartScreen
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VisualElementsRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.StartScreen.SecondaryTileVisualElements> AlternateVisualElements
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<SecondaryTileVisualElements> VisualElementsRequest.AlternateVisualElements is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset Deadline
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset VisualElementsRequest.Deadline is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.StartScreen.SecondaryTileVisualElements VisualElements
		{
			get
			{
				throw new global::System.NotImplementedException("The member SecondaryTileVisualElements VisualElementsRequest.VisualElements is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.StartScreen.VisualElementsRequest.VisualElements.get
		// Forced skipping of method Windows.UI.StartScreen.VisualElementsRequest.AlternateVisualElements.get
		// Forced skipping of method Windows.UI.StartScreen.VisualElementsRequest.Deadline.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.StartScreen.VisualElementsRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member VisualElementsRequestDeferral VisualElementsRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
