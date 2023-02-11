#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Printers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IppIntegerRange 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int End
		{
			get
			{
				throw new global::System.NotImplementedException("The member int IppIntegerRange.End is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20IppIntegerRange.End");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Start
		{
			get
			{
				throw new global::System.NotImplementedException("The member int IppIntegerRange.Start is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20IppIntegerRange.Start");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public IppIntegerRange( int start,  int end) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.IppIntegerRange", "IppIntegerRange.IppIntegerRange(int start, int end)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Printers.IppIntegerRange.IppIntegerRange(int, int)
		// Forced skipping of method Windows.Devices.Printers.IppIntegerRange.Start.get
		// Forced skipping of method Windows.Devices.Printers.IppIntegerRange.End.get
	}
}
