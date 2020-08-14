#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Cortana
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CortanaActionableInsightsOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SurroundingText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CortanaActionableInsightsOptions.SurroundingText is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Cortana.CortanaActionableInsightsOptions", "string CortanaActionableInsightsOptions.SurroundingText");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri ContentSourceWebLink
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri CortanaActionableInsightsOptions.ContentSourceWebLink is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Cortana.CortanaActionableInsightsOptions", "Uri CortanaActionableInsightsOptions.ContentSourceWebLink");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CortanaActionableInsightsOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Cortana.CortanaActionableInsightsOptions", "CortanaActionableInsightsOptions.CortanaActionableInsightsOptions()");
		}
		#endif
		// Forced skipping of method Windows.Services.Cortana.CortanaActionableInsightsOptions.CortanaActionableInsightsOptions()
		// Forced skipping of method Windows.Services.Cortana.CortanaActionableInsightsOptions.ContentSourceWebLink.get
		// Forced skipping of method Windows.Services.Cortana.CortanaActionableInsightsOptions.ContentSourceWebLink.set
		// Forced skipping of method Windows.Services.Cortana.CortanaActionableInsightsOptions.SurroundingText.get
		// Forced skipping of method Windows.Services.Cortana.CortanaActionableInsightsOptions.SurroundingText.set
	}
}
