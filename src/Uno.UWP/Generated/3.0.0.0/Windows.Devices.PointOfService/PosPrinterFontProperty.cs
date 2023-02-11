#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PosPrinterFontProperty 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.PointOfService.SizeUInt32> CharacterSizes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<SizeUInt32> PosPrinterFontProperty.CharacterSizes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CSizeUInt32%3E%20PosPrinterFontProperty.CharacterSizes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsScalableToAnySize
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PosPrinterFontProperty.IsScalableToAnySize is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PosPrinterFontProperty.IsScalableToAnySize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TypeFace
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PosPrinterFontProperty.TypeFace is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PosPrinterFontProperty.TypeFace");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.PosPrinterFontProperty.TypeFace.get
		// Forced skipping of method Windows.Devices.PointOfService.PosPrinterFontProperty.IsScalableToAnySize.get
		// Forced skipping of method Windows.Devices.PointOfService.PosPrinterFontProperty.CharacterSizes.get
	}
}
