#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GraphicsCaptureItem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string GraphicsCaptureItem.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.SizeInt32 Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member SizeInt32 GraphicsCaptureItem.Size is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.DisplayName.get
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.Size.get
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.Closed.add
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.Closed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Capture.GraphicsCaptureItem CreateFromVisual( global::Windows.UI.Composition.Visual visual)
		{
			throw new global::System.NotImplementedException("The member GraphicsCaptureItem GraphicsCaptureItem.CreateFromVisual(Visual visual) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Capture.GraphicsCaptureItem, object> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureItem", "event TypedEventHandler<GraphicsCaptureItem, object> GraphicsCaptureItem.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureItem", "event TypedEventHandler<GraphicsCaptureItem, object> GraphicsCaptureItem.Closed");
			}
		}
		#endif
	}
}
