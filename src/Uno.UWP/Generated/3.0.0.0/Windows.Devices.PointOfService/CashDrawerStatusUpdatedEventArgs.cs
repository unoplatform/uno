#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CashDrawerStatusUpdatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.CashDrawerStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member CashDrawerStatus CashDrawerStatusUpdatedEventArgs.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CashDrawerStatus%20CashDrawerStatusUpdatedEventArgs.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerStatusUpdatedEventArgs.Status.get
	}
}
