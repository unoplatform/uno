#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ESimDownloadProfileMetadataResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ESimProfileMetadata ProfileMetadata
		{
			get
			{
				throw new global::System.NotImplementedException("The member ESimProfileMetadata ESimDownloadProfileMetadataResult.ProfileMetadata is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ESimOperationResult Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member ESimOperationResult ESimDownloadProfileMetadataResult.Result is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimDownloadProfileMetadataResult.Result.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimDownloadProfileMetadataResult.ProfileMetadata.get
	}
}
