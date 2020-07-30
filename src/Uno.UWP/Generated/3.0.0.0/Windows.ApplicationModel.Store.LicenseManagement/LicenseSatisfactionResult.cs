#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.LicenseManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LicenseSatisfactionResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception LicenseSatisfactionResult.ExtendedError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.ApplicationModel.Store.LicenseManagement.LicenseSatisfactionInfo> LicenseSatisfactionInfos
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, LicenseSatisfactionInfo> LicenseSatisfactionResult.LicenseSatisfactionInfos is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.LicenseManagement.LicenseSatisfactionResult.LicenseSatisfactionInfos.get
		// Forced skipping of method Windows.ApplicationModel.Store.LicenseManagement.LicenseSatisfactionResult.ExtendedError.get
	}
}
