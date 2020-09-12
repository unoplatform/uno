#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Profile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlatformDiagnosticsAndUsageDataSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Profile.PlatformDataCollectionLevel CollectionLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlatformDataCollectionLevel PlatformDiagnosticsAndUsageDataSettings.CollectionLevel is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Profile.PlatformDiagnosticsAndUsageDataSettings.CollectionLevel.get
		// Forced skipping of method Windows.System.Profile.PlatformDiagnosticsAndUsageDataSettings.CollectionLevelChanged.add
		// Forced skipping of method Windows.System.Profile.PlatformDiagnosticsAndUsageDataSettings.CollectionLevelChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool CanCollectDiagnostics( global::Windows.System.Profile.PlatformDataCollectionLevel level)
		{
			throw new global::System.NotImplementedException("The member bool PlatformDiagnosticsAndUsageDataSettings.CanCollectDiagnostics(PlatformDataCollectionLevel level) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> CollectionLevelChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Profile.PlatformDiagnosticsAndUsageDataSettings", "event EventHandler<object> PlatformDiagnosticsAndUsageDataSettings.CollectionLevelChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Profile.PlatformDiagnosticsAndUsageDataSettings", "event EventHandler<object> PlatformDiagnosticsAndUsageDataSettings.CollectionLevelChanged");
			}
		}
		#endif
	}
}
