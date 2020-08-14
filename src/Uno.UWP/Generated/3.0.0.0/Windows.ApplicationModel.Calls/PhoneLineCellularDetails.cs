#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneLineCellularDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsModemOn
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PhoneLineCellularDetails.IsModemOn is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int RegistrationRejectCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PhoneLineCellularDetails.RegistrationRejectCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int SimSlotIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PhoneLineCellularDetails.SimSlotIndex is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneSimState SimState
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhoneSimState PhoneLineCellularDetails.SimState is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineCellularDetails.SimState.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineCellularDetails.SimSlotIndex.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineCellularDetails.IsModemOn.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineCellularDetails.RegistrationRejectCode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetNetworkOperatorDisplayText( global::Windows.ApplicationModel.Calls.PhoneLineNetworkOperatorDisplayTextLocation location)
		{
			throw new global::System.NotImplementedException("The member string PhoneLineCellularDetails.GetNetworkOperatorDisplayText(PhoneLineNetworkOperatorDisplayTextLocation location) is not implemented in Uno.");
		}
		#endif
	}
}
