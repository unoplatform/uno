#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SmsDevice2 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SmscAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SmsDevice2.SmscAddress is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sms.SmsDevice2", "string SmsDevice2.SmscAddress");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AccountPhoneNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SmsDevice2.AccountPhoneNumber is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sms.CellularClass CellularClass
		{
			get
			{
				throw new global::System.NotImplementedException("The member CellularClass SmsDevice2.CellularClass is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SmsDevice2.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sms.SmsDeviceStatus DeviceStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member SmsDeviceStatus SmsDevice2.DeviceStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ParentDeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SmsDevice2.ParentDeviceId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.SmscAddress.get
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.SmscAddress.set
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.DeviceId.get
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.ParentDeviceId.get
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.AccountPhoneNumber.get
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.CellularClass.get
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.DeviceStatus.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sms.SmsEncodedLength CalculateLength( global::Windows.Devices.Sms.ISmsMessageBase message)
		{
			throw new global::System.NotImplementedException("The member SmsEncodedLength SmsDevice2.CalculateLength(ISmsMessageBase message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Sms.SmsSendMessageResult> SendMessageAndGetResultAsync( global::Windows.Devices.Sms.ISmsMessageBase message)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SmsSendMessageResult> SmsDevice2.SendMessageAndGetResultAsync(ISmsMessageBase message) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.DeviceStatusChanged.add
		// Forced skipping of method Windows.Devices.Sms.SmsDevice2.DeviceStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string SmsDevice2.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Sms.SmsDevice2 FromId( string deviceId)
		{
			throw new global::System.NotImplementedException("The member SmsDevice2 SmsDevice2.FromId(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Sms.SmsDevice2 GetDefault()
		{
			throw new global::System.NotImplementedException("The member SmsDevice2 SmsDevice2.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Sms.SmsDevice2 FromParentId( string parentDeviceId)
		{
			throw new global::System.NotImplementedException("The member SmsDevice2 SmsDevice2.FromParentId(string parentDeviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sms.SmsDevice2, object> DeviceStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sms.SmsDevice2", "event TypedEventHandler<SmsDevice2, object> SmsDevice2.DeviceStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sms.SmsDevice2", "event TypedEventHandler<SmsDevice2, object> SmsDevice2.DeviceStatusChanged");
			}
		}
		#endif
	}
}
