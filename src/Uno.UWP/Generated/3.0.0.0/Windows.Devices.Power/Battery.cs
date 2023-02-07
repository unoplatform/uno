#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Power
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class Battery 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Battery.DeviceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20Battery.DeviceId");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Power.Battery AggregateBattery
		{
			get
			{
				throw new global::System.NotImplementedException("The member Battery Battery.AggregateBattery is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Battery%20Battery.AggregateBattery");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Power.Battery.DeviceId.get
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Power.BatteryReport GetReport()
		{
			throw new global::System.NotImplementedException("The member BatteryReport Battery.GetReport() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BatteryReport%20Battery.GetReport%28%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.Power.Battery.ReportUpdated.add
		// Forced skipping of method Windows.Devices.Power.Battery.ReportUpdated.remove
		// Forced skipping of method Windows.Devices.Power.Battery.AggregateBattery.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Power.Battery> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Battery> Battery.FromIdAsync(string deviceId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CBattery%3E%20Battery.FromIdAsync%28string%20deviceId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string Battery.GetDeviceSelector() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20Battery.GetDeviceSelector%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Power.Battery, object> ReportUpdated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Power.Battery", "event TypedEventHandler<Battery, object> Battery.ReportUpdated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Power.Battery", "event TypedEventHandler<Battery, object> Battery.ReportUpdated");
			}
		}
		#endif
	}
}
