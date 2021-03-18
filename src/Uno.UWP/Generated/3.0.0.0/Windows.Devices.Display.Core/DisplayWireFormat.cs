#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayWireFormat 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int BitsPerChannel
		{
			get
			{
				throw new global::System.NotImplementedException("The member int DisplayWireFormat.BitsPerChannel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayWireFormatColorSpace ColorSpace
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayWireFormatColorSpace DisplayWireFormat.ColorSpace is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayWireFormatEotf Eotf
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayWireFormatEotf DisplayWireFormat.Eotf is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayWireFormatHdrMetadata HdrMetadata
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayWireFormatHdrMetadata DisplayWireFormat.HdrMetadata is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayWireFormatPixelEncoding PixelEncoding
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayWireFormatPixelEncoding DisplayWireFormat.PixelEncoding is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, object> DisplayWireFormat.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DisplayWireFormat( global::Windows.Devices.Display.Core.DisplayWireFormatPixelEncoding pixelEncoding,  int bitsPerChannel,  global::Windows.Devices.Display.Core.DisplayWireFormatColorSpace colorSpace,  global::Windows.Devices.Display.Core.DisplayWireFormatEotf eotf,  global::Windows.Devices.Display.Core.DisplayWireFormatHdrMetadata hdrMetadata) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Display.Core.DisplayWireFormat", "DisplayWireFormat.DisplayWireFormat(DisplayWireFormatPixelEncoding pixelEncoding, int bitsPerChannel, DisplayWireFormatColorSpace colorSpace, DisplayWireFormatEotf eotf, DisplayWireFormatHdrMetadata hdrMetadata)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayWireFormat.DisplayWireFormat(Windows.Devices.Display.Core.DisplayWireFormatPixelEncoding, int, Windows.Devices.Display.Core.DisplayWireFormatColorSpace, Windows.Devices.Display.Core.DisplayWireFormatEotf, Windows.Devices.Display.Core.DisplayWireFormatHdrMetadata)
		// Forced skipping of method Windows.Devices.Display.Core.DisplayWireFormat.PixelEncoding.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayWireFormat.BitsPerChannel.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayWireFormat.ColorSpace.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayWireFormat.Eotf.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayWireFormat.HdrMetadata.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayWireFormat.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Display.Core.DisplayWireFormat CreateWithProperties( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<global::System.Guid, object>> extraProperties,  global::Windows.Devices.Display.Core.DisplayWireFormatPixelEncoding pixelEncoding,  int bitsPerChannel,  global::Windows.Devices.Display.Core.DisplayWireFormatColorSpace colorSpace,  global::Windows.Devices.Display.Core.DisplayWireFormatEotf eotf,  global::Windows.Devices.Display.Core.DisplayWireFormatHdrMetadata hdrMetadata)
		{
			throw new global::System.NotImplementedException("The member DisplayWireFormat DisplayWireFormat.CreateWithProperties(IEnumerable<KeyValuePair<Guid, object>> extraProperties, DisplayWireFormatPixelEncoding pixelEncoding, int bitsPerChannel, DisplayWireFormatColorSpace colorSpace, DisplayWireFormatEotf eotf, DisplayWireFormatHdrMetadata hdrMetadata) is not implemented in Uno.");
		}
		#endif
	}
}
