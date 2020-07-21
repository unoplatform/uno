#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CashDrawerCloseAlarm 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint BeepFrequency
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CashDrawerCloseAlarm.BeepFrequency is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerCloseAlarm", "uint CashDrawerCloseAlarm.BeepFrequency");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan BeepDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan CashDrawerCloseAlarm.BeepDuration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerCloseAlarm", "TimeSpan CashDrawerCloseAlarm.BeepDuration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan BeepDelay
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan CashDrawerCloseAlarm.BeepDelay is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerCloseAlarm", "TimeSpan CashDrawerCloseAlarm.BeepDelay");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan AlarmTimeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan CashDrawerCloseAlarm.AlarmTimeout is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerCloseAlarm", "TimeSpan CashDrawerCloseAlarm.AlarmTimeout");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.AlarmTimeout.set
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.AlarmTimeout.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.BeepFrequency.set
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.BeepFrequency.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.BeepDuration.set
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.BeepDuration.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.BeepDelay.set
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.BeepDelay.get
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.AlarmTimeoutExpired.add
		// Forced skipping of method Windows.Devices.PointOfService.CashDrawerCloseAlarm.AlarmTimeoutExpired.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> CashDrawerCloseAlarm.StartAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.PointOfService.CashDrawerCloseAlarm, object> AlarmTimeoutExpired
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerCloseAlarm", "event TypedEventHandler<CashDrawerCloseAlarm, object> CashDrawerCloseAlarm.AlarmTimeoutExpired");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.CashDrawerCloseAlarm", "event TypedEventHandler<CashDrawerCloseAlarm, object> CashDrawerCloseAlarm.AlarmTimeoutExpired");
			}
		}
		#endif
	}
}
