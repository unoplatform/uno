#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISmsMessageBase 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.Sms.CellularClass CellularClass
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DeviceId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.Sms.SmsMessageClass MessageClass
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.Sms.SmsMessageType MessageType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string SimIccId
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Devices.Sms.ISmsMessageBase.MessageType.get
		// Forced skipping of method Windows.Devices.Sms.ISmsMessageBase.DeviceId.get
		// Forced skipping of method Windows.Devices.Sms.ISmsMessageBase.CellularClass.get
		// Forced skipping of method Windows.Devices.Sms.ISmsMessageBase.MessageClass.get
		// Forced skipping of method Windows.Devices.Sms.ISmsMessageBase.SimIccId.get
	}
}
