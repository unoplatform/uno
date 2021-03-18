#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteLauncherOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri FallbackUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri RemoteLauncherOptions.FallbackUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteLauncherOptions", "Uri RemoteLauncherOptions.FallbackUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> PreferredAppIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> RemoteLauncherOptions.PreferredAppIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteLauncherOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteLauncherOptions", "RemoteLauncherOptions.RemoteLauncherOptions()");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteLauncherOptions.RemoteLauncherOptions()
		// Forced skipping of method Windows.System.RemoteLauncherOptions.FallbackUri.get
		// Forced skipping of method Windows.System.RemoteLauncherOptions.FallbackUri.set
		// Forced skipping of method Windows.System.RemoteLauncherOptions.PreferredAppIds.get
	}
}
