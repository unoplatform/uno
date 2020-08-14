#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LineDisplayCustomGlyphs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Size SizeInPixels
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size LineDisplayCustomGlyphs.SizeInPixels is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<uint> SupportedGlyphCodes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<uint> LineDisplayCustomGlyphs.SupportedGlyphCodes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayCustomGlyphs.SizeInPixels.get
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayCustomGlyphs.SupportedGlyphCodes.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryRedefineAsync( uint glyphCode,  global::Windows.Storage.Streams.IBuffer glyphData)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayCustomGlyphs.TryRedefineAsync(uint glyphCode, IBuffer glyphData) is not implemented in Uno.");
		}
		#endif
	}
}
