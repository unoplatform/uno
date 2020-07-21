#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICommonPosPrintStationCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.PointOfService.PosPrinterCartridgeSensors CartridgeSensors
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.PointOfService.PosPrinterColorCapabilities ColorCartridgeCapabilities
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsBoldSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsDoubleHighDoubleWidePrintSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsDoubleHighPrintSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsDoubleWidePrintSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsDualColorSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsItalicSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsPaperEmptySensorSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsPaperNearEndSensorSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsPrinterPresent
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsUnderlineSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<uint> SupportedCharactersPerLine
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsPrinterPresent.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsDualColorSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.ColorCartridgeCapabilities.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.CartridgeSensors.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsBoldSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsItalicSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsUnderlineSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsDoubleHighPrintSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsDoubleWidePrintSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsDoubleHighDoubleWidePrintSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsPaperEmptySensorSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.IsPaperNearEndSensorSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities.SupportedCharactersPerLine.get
	}
}
