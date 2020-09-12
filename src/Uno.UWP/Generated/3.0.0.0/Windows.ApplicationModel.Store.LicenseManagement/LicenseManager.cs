#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.LicenseManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LicenseManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction RefreshLicensesAsync( global::Windows.ApplicationModel.Store.LicenseManagement.LicenseRefreshOption refreshOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction LicenseManager.RefreshLicensesAsync(LicenseRefreshOption refreshOption) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AddLicenseAsync( global::Windows.Storage.Streams.IBuffer license)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction LicenseManager.AddLicenseAsync(IBuffer license) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.LicenseManagement.LicenseSatisfactionResult> GetSatisfactionInfosAsync( global::System.Collections.Generic.IEnumerable<string> contentIds,  global::System.Collections.Generic.IEnumerable<string> keyIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LicenseSatisfactionResult> LicenseManager.GetSatisfactionInfosAsync(IEnumerable<string> contentIds, IEnumerable<string> keyIds) is not implemented in Uno.");
		}
		#endif
	}
}
