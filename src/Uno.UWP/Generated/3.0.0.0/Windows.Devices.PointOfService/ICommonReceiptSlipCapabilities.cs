#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICommonReceiptSlipCapabilities : global::Windows.Devices.PointOfService.ICommonPosPrintStationCapabilities
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Is180RotationSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsBarcodeSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsBitmapSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsLeft90RotationSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsPrintAreaSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsRight90RotationSupported
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.PointOfService.PosPrinterRuledLineCapabilities RuledLineCapabilities
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.PointOfService.PosPrinterRotation> SupportedBarcodeRotations
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.PointOfService.PosPrinterRotation> SupportedBitmapRotations
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.IsBarcodeSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.IsBitmapSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.IsLeft90RotationSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.IsRight90RotationSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.Is180RotationSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.IsPrintAreaSupported.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.RuledLineCapabilities.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.SupportedBarcodeRotations.get
		// Forced skipping of method Windows.Devices.PointOfService.ICommonReceiptSlipCapabilities.SupportedBitmapRotations.get
	}
}
