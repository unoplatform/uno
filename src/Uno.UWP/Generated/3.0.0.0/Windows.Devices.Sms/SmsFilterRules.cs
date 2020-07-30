#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SmsFilterRules 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sms.SmsFilterActionType ActionType
		{
			get
			{
				throw new global::System.NotImplementedException("The member SmsFilterActionType SmsFilterRules.ActionType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Devices.Sms.SmsFilterRule> Rules
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<SmsFilterRule> SmsFilterRules.Rules is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SmsFilterRules( global::Windows.Devices.Sms.SmsFilterActionType actionType) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sms.SmsFilterRules", "SmsFilterRules.SmsFilterRules(SmsFilterActionType actionType)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sms.SmsFilterRules.SmsFilterRules(Windows.Devices.Sms.SmsFilterActionType)
		// Forced skipping of method Windows.Devices.Sms.SmsFilterRules.ActionType.get
		// Forced skipping of method Windows.Devices.Sms.SmsFilterRules.Rules.get
	}
}
