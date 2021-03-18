#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CashDrawerClosedEventArgs : global::Windows.Devices.PointOfService.ICashDrawerEventSourceEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.CashDrawer CashDrawer
		{
			get
			{
				throw new global::System.NotImplementedException("The member CashDrawer CashDrawerClosedEventArgs.CashDrawer is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerClosedEventArgs.CashDrawer.get
		// Processing: Windows.Devices.PointOfService.ICashDrawerEventSourceEventArgs
	}
}
