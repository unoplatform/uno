#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Update
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PreviewBuildsManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ArePreviewBuildsAllowed
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PreviewBuildsManager.ArePreviewBuildsAllowed is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Update.PreviewBuildsManager", "bool PreviewBuildsManager.ArePreviewBuildsAllowed");
			}
		}
		#endif
		// Forced skipping of method Windows.Management.Update.PreviewBuildsManager.ArePreviewBuildsAllowed.get
		// Forced skipping of method Windows.Management.Update.PreviewBuildsManager.ArePreviewBuildsAllowed.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Management.Update.PreviewBuildsState GetCurrentState()
		{
			throw new global::System.NotImplementedException("The member PreviewBuildsState PreviewBuildsManager.GetCurrentState() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> SyncAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> PreviewBuildsManager.SyncAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Management.Update.PreviewBuildsManager GetDefault()
		{
			throw new global::System.NotImplementedException("The member PreviewBuildsManager PreviewBuildsManager.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool PreviewBuildsManager.IsSupported() is not implemented in Uno.");
		}
		#endif
	}
}
