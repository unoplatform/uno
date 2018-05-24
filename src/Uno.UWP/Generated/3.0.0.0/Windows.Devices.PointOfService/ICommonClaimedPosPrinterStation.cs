#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICommonClaimedPosPrinterStation 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint CharactersPerLine
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Devices.PointOfService.PosPrinterColorCartridge ColorCartridge
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsCartridgeEmpty
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsCartridgeRemoved
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsCoverOpen
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsHeadCleaning
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsLetterQuality
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsPaperEmpty
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsPaperNearEnd
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsReadyToPrint
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint LineHeight
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint LineSpacing
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint LineWidth
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.CharactersPerLine.set
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.CharactersPerLine.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.LineHeight.set
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.LineHeight.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.LineSpacing.set
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.LineSpacing.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.LineWidth.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsLetterQuality.set
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsLetterQuality.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsPaperNearEnd.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.ColorCartridge.set
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.ColorCartridge.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsCoverOpen.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsCartridgeRemoved.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsCartridgeEmpty.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsHeadCleaning.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsPaperEmpty.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonClaimedPosPrinterStation.IsReadyToPrint.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool ValidateData( string data);
		#endif
	}
}
