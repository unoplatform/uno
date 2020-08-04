#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScannerReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer ScanData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer BarcodeScannerReport.ScanData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer ScanDataLabel
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer BarcodeScannerReport.ScanDataLabel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ScanDataType
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint BarcodeScannerReport.ScanDataType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BarcodeScannerReport( uint scanDataType,  global::Windows.Storage.Streams.IBuffer scanData,  global::Windows.Storage.Streams.IBuffer scanDataLabel) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.BarcodeScannerReport", "BarcodeScannerReport.BarcodeScannerReport(uint scanDataType, IBuffer scanData, IBuffer scanDataLabel)");
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScannerReport.BarcodeScannerReport(uint, Windows.Storage.Streams.IBuffer, Windows.Storage.Streams.IBuffer)
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScannerReport.ScanDataType.get
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScannerReport.ScanData.get
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScannerReport.ScanDataLabel.get
	}
}
