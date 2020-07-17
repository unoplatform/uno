#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadialControllerMenuItem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Tag
		{
			get
			{
				throw new global::System.NotImplementedException("The member object RadialControllerMenuItem.Tag is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerMenuItem", "object RadialControllerMenuItem.Tag");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RadialControllerMenuItem.DisplayText is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.RadialControllerMenuItem.DisplayText.get
		// Forced skipping of method Windows.UI.Input.RadialControllerMenuItem.Tag.get
		// Forced skipping of method Windows.UI.Input.RadialControllerMenuItem.Tag.set
		// Forced skipping of method Windows.UI.Input.RadialControllerMenuItem.Invoked.add
		// Forced skipping of method Windows.UI.Input.RadialControllerMenuItem.Invoked.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.RadialControllerMenuItem CreateFromFontGlyph( string displayText,  string glyph,  string fontFamily)
		{
			throw new global::System.NotImplementedException("The member RadialControllerMenuItem RadialControllerMenuItem.CreateFromFontGlyph(string displayText, string glyph, string fontFamily) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.RadialControllerMenuItem CreateFromFontGlyph( string displayText,  string glyph,  string fontFamily,  global::System.Uri fontUri)
		{
			throw new global::System.NotImplementedException("The member RadialControllerMenuItem RadialControllerMenuItem.CreateFromFontGlyph(string displayText, string glyph, string fontFamily, Uri fontUri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.RadialControllerMenuItem CreateFromIcon( string displayText,  global::Windows.Storage.Streams.RandomAccessStreamReference icon)
		{
			throw new global::System.NotImplementedException("The member RadialControllerMenuItem RadialControllerMenuItem.CreateFromIcon(string displayText, RandomAccessStreamReference icon) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.RadialControllerMenuItem CreateFromKnownIcon( string displayText,  global::Windows.UI.Input.RadialControllerMenuKnownIcon value)
		{
			throw new global::System.NotImplementedException("The member RadialControllerMenuItem RadialControllerMenuItem.CreateFromKnownIcon(string displayText, RadialControllerMenuKnownIcon value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialControllerMenuItem, object> Invoked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerMenuItem", "event TypedEventHandler<RadialControllerMenuItem, object> RadialControllerMenuItem.Invoked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerMenuItem", "event TypedEventHandler<RadialControllerMenuItem, object> RadialControllerMenuItem.Invoked");
			}
		}
		#endif
	}
}
