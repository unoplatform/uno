#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SmsSendMessageResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sms.CellularClass CellularClass
		{
			get
			{
				throw new global::System.NotImplementedException("The member CellularClass SmsSendMessageResult.CellularClass is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CellularClass%20SmsSendMessageResult.CellularClass");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsErrorTransient
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SmsSendMessageResult.IsErrorTransient is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20SmsSendMessageResult.IsErrorTransient");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSuccessful
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SmsSendMessageResult.IsSuccessful is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20SmsSendMessageResult.IsSuccessful");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<int> MessageReferenceNumbers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<int> SmsSendMessageResult.MessageReferenceNumbers is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cint%3E%20SmsSendMessageResult.MessageReferenceNumbers");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sms.SmsModemErrorCode ModemErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member SmsModemErrorCode SmsSendMessageResult.ModemErrorCode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SmsModemErrorCode%20SmsSendMessageResult.ModemErrorCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int NetworkCauseCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member int SmsSendMessageResult.NetworkCauseCode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20SmsSendMessageResult.NetworkCauseCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int TransportFailureCause
		{
			get
			{
				throw new global::System.NotImplementedException("The member int SmsSendMessageResult.TransportFailureCause is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20SmsSendMessageResult.TransportFailureCause");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sms.SmsSendMessageResult.IsSuccessful.get
		// Forced skipping of method Windows.Devices.Sms.SmsSendMessageResult.MessageReferenceNumbers.get
		// Forced skipping of method Windows.Devices.Sms.SmsSendMessageResult.CellularClass.get
		// Forced skipping of method Windows.Devices.Sms.SmsSendMessageResult.ModemErrorCode.get
		// Forced skipping of method Windows.Devices.Sms.SmsSendMessageResult.IsErrorTransient.get
		// Forced skipping of method Windows.Devices.Sms.SmsSendMessageResult.NetworkCauseCode.get
		// Forced skipping of method Windows.Devices.Sms.SmsSendMessageResult.TransportFailureCause.get
	}
}
