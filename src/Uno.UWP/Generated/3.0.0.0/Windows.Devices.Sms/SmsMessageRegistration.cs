#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SmsMessageRegistration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SmsMessageRegistration.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Sms.SmsMessageRegistration> AllRegistrations
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<SmsMessageRegistration> SmsMessageRegistration.AllRegistrations is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sms.SmsMessageRegistration.Id.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Unregister()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sms.SmsMessageRegistration", "void SmsMessageRegistration.Unregister()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sms.SmsMessageRegistration.MessageReceived.add
		// Forced skipping of method Windows.Devices.Sms.SmsMessageRegistration.MessageReceived.remove
		// Forced skipping of method Windows.Devices.Sms.SmsMessageRegistration.AllRegistrations.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Sms.SmsMessageRegistration Register( string id,  global::Windows.Devices.Sms.SmsFilterRules filterRules)
		{
			throw new global::System.NotImplementedException("The member SmsMessageRegistration SmsMessageRegistration.Register(string id, SmsFilterRules filterRules) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sms.SmsMessageRegistration, global::Windows.Devices.Sms.SmsMessageReceivedTriggerDetails> MessageReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sms.SmsMessageRegistration", "event TypedEventHandler<SmsMessageRegistration, SmsMessageReceivedTriggerDetails> SmsMessageRegistration.MessageReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sms.SmsMessageRegistration", "event TypedEventHandler<SmsMessageRegistration, SmsMessageReceivedTriggerDetails> SmsMessageRegistration.MessageReceived");
			}
		}
		#endif
	}
}
