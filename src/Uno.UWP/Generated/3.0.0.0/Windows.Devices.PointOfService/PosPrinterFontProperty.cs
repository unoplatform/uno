#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PosPrinterFontProperty 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.PointOfService.SizeUInt32> CharacterSizes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<SizeUInt32> PosPrinterFontProperty.CharacterSizes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsScalableToAnySize
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PosPrinterFontProperty.IsScalableToAnySize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string TypeFace
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PosPrinterFontProperty.TypeFace is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.PosPrinterFontProperty.TypeFace.get
		// Forced skipping of method Windows.Devices.PointOfService.PosPrinterFontProperty.IsScalableToAnySize.get
		// Forced skipping of method Windows.Devices.PointOfService.PosPrinterFontProperty.CharacterSizes.get
	}
}
