#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CashDrawerEventSource 
	{
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerEventSource.DrawerClosed.add
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerEventSource.DrawerClosed.remove
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerEventSource.DrawerOpened.add
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerEventSource.DrawerOpened.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.PointOfService.CashDrawerEventSource, global::Windows.Devices.PointOfService.CashDrawerClosedEventArgs> DrawerClosed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerEventSource", "event TypedEventHandler<CashDrawerEventSource, CashDrawerClosedEventArgs> CashDrawerEventSource.DrawerClosed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerEventSource", "event TypedEventHandler<CashDrawerEventSource, CashDrawerClosedEventArgs> CashDrawerEventSource.DrawerClosed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.PointOfService.CashDrawerEventSource, global::Windows.Devices.PointOfService.CashDrawerOpenedEventArgs> DrawerOpened
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerEventSource", "event TypedEventHandler<CashDrawerEventSource, CashDrawerOpenedEventArgs> CashDrawerEventSource.DrawerOpened");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerEventSource", "event TypedEventHandler<CashDrawerEventSource, CashDrawerOpenedEventArgs> CashDrawerEventSource.DrawerOpened");
			}
		}
		#endif
	}
}
