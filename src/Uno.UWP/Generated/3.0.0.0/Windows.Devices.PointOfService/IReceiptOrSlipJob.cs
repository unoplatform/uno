#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IReceiptOrSlipJob : global::Windows.Devices.PointOfService.IPosPrinterJob
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetBarcodeRotation( global::Windows.Devices.PointOfService.PosPrinterRotation value);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetPrintRotation( global::Windows.Devices.PointOfService.PosPrinterRotation value,  bool includeBitmaps);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetPrintArea( global::Windows.Foundation.Rect value);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment,  uint width);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetCustomAlignedBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetCustomAlignedBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance,  uint width);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void PrintSavedBitmap( uint bitmapNumber);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void DrawRuledLine( string positionList,  global::Windows.Devices.PointOfService.PosPrinterLineDirection lineDirection,  uint lineWidth,  global::Windows.Devices.PointOfService.PosPrinterLineStyle lineStyle,  uint lineColor);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void PrintBarcode( string data,  uint symbology,  uint height,  uint width,  global::Windows.Devices.PointOfService.PosPrinterBarcodeTextPosition textPosition,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void PrintBarcodeCustomAlign( string data,  uint symbology,  uint height,  uint width,  global::Windows.Devices.PointOfService.PosPrinterBarcodeTextPosition textPosition,  uint alignmentDistance);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void PrintBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void PrintBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment,  uint width);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void PrintCustomAlignedBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void PrintCustomAlignedBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance,  uint width);
		#endif
	}
}
