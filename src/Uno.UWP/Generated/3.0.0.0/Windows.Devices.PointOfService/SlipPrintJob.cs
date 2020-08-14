#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SlipPrintJob : global::Windows.Devices.PointOfService.IReceiptOrSlipJob,global::Windows.Devices.PointOfService.IPosPrinterJob
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Print( string data,  global::Windows.Devices.PointOfService.PosPrinterPrintOptions printOptions)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.Print(string data, PosPrinterPrintOptions printOptions)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void FeedPaperByLine( int lineCount)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.FeedPaperByLine(int lineCount)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void FeedPaperByMapModeUnit( int distance)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.FeedPaperByMapModeUnit(int distance)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBarcodeRotation( global::Windows.Devices.PointOfService.PosPrinterRotation value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.SetBarcodeRotation(PosPrinterRotation value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPrintRotation( global::Windows.Devices.PointOfService.PosPrinterRotation value,  bool includeBitmaps)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.SetPrintRotation(PosPrinterRotation value, bool includeBitmaps)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPrintArea( global::Windows.Foundation.Rect value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.SetPrintArea(Rect value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.SetBitmap(uint bitmapNumber, BitmapFrame bitmap, PosPrinterAlignment alignment)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment,  uint width)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.SetBitmap(uint bitmapNumber, BitmapFrame bitmap, PosPrinterAlignment alignment, uint width)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetCustomAlignedBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.SetCustomAlignedBitmap(uint bitmapNumber, BitmapFrame bitmap, uint alignmentDistance)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetCustomAlignedBitmap( uint bitmapNumber,  global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance,  uint width)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.SetCustomAlignedBitmap(uint bitmapNumber, BitmapFrame bitmap, uint alignmentDistance, uint width)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintSavedBitmap( uint bitmapNumber)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintSavedBitmap(uint bitmapNumber)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DrawRuledLine( string positionList,  global::Windows.Devices.PointOfService.PosPrinterLineDirection lineDirection,  uint lineWidth,  global::Windows.Devices.PointOfService.PosPrinterLineStyle lineStyle,  uint lineColor)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.DrawRuledLine(string positionList, PosPrinterLineDirection lineDirection, uint lineWidth, PosPrinterLineStyle lineStyle, uint lineColor)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintBarcode( string data,  uint symbology,  uint height,  uint width,  global::Windows.Devices.PointOfService.PosPrinterBarcodeTextPosition textPosition,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintBarcode(string data, uint symbology, uint height, uint width, PosPrinterBarcodeTextPosition textPosition, PosPrinterAlignment alignment)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintBarcodeCustomAlign( string data,  uint symbology,  uint height,  uint width,  global::Windows.Devices.PointOfService.PosPrinterBarcodeTextPosition textPosition,  uint alignmentDistance)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintBarcodeCustomAlign(string data, uint symbology, uint height, uint width, PosPrinterBarcodeTextPosition textPosition, uint alignmentDistance)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintBitmap(BitmapFrame bitmap, PosPrinterAlignment alignment)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  global::Windows.Devices.PointOfService.PosPrinterAlignment alignment,  uint width)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintBitmap(BitmapFrame bitmap, PosPrinterAlignment alignment, uint width)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintCustomAlignedBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintCustomAlignedBitmap(BitmapFrame bitmap, uint alignmentDistance)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintCustomAlignedBitmap( global::Windows.Graphics.Imaging.BitmapFrame bitmap,  uint alignmentDistance,  uint width)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintCustomAlignedBitmap(BitmapFrame bitmap, uint alignmentDistance, uint width)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Print( string data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.Print(string data)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintLine( string data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintLine(string data)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PrintLine()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.SlipPrintJob", "void SlipPrintJob.PrintLine()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ExecuteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SlipPrintJob.ExecuteAsync() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Devices.PointOfService.IReceiptOrSlipJob
		// Processing: Windows.Devices.PointOfService.IPosPrinterJob
	}
}
