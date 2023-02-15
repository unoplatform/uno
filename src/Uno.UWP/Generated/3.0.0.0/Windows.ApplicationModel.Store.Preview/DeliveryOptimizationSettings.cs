#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeliveryOptimizationSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Store.Preview.DeliveryOptimizationDownloadMode DownloadMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeliveryOptimizationDownloadMode DeliveryOptimizationSettings.DownloadMode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DeliveryOptimizationDownloadMode%20DeliveryOptimizationSettings.DownloadMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Store.Preview.DeliveryOptimizationDownloadModeSource DownloadModeSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeliveryOptimizationDownloadModeSource DeliveryOptimizationSettings.DownloadModeSource is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DeliveryOptimizationDownloadModeSource%20DeliveryOptimizationSettings.DownloadModeSource");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.DeliveryOptimizationSettings.DownloadMode.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.DeliveryOptimizationSettings.DownloadModeSource.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Store.Preview.DeliveryOptimizationSettings GetCurrentSettings()
		{
			throw new global::System.NotImplementedException("The member DeliveryOptimizationSettings DeliveryOptimizationSettings.GetCurrentSettings() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DeliveryOptimizationSettings%20DeliveryOptimizationSettings.GetCurrentSettings%28%29");
		}
		#endif
	}
}
