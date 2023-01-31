#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.PrintSupport
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintSupportSessionInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppPrintDevice Printer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IppPrintDevice PrintSupportSessionInfo.Printer is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppPrintDevice%20PrintSupportSessionInfo.Printer");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.AppInfo SourceAppInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppInfo PrintSupportSessionInfo.SourceAppInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInfo%20PrintSupportSessionInfo.SourceAppInfo");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportSessionInfo.SourceAppInfo.get
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportSessionInfo.Printer.get
	}
}
