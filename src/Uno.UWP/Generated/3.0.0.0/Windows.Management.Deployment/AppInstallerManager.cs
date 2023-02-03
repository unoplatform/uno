#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppInstallerManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetAutoUpdateSettings( string packageFamilyName,  global::Windows.Management.Deployment.AutoUpdateSettingsOptions appInstallerInfo)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.AppInstallerManager", "void AppInstallerManager.SetAutoUpdateSettings(string packageFamilyName, AutoUpdateSettingsOptions appInstallerInfo)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ClearAutoUpdateSettings( string packageFamilyName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.AppInstallerManager", "void AppInstallerManager.ClearAutoUpdateSettings(string packageFamilyName)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PauseAutoUpdatesUntil( string packageFamilyName,  global::System.DateTimeOffset dateTime)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.AppInstallerManager", "void AppInstallerManager.PauseAutoUpdatesUntil(string packageFamilyName, DateTimeOffset dateTime)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Management.Deployment.AppInstallerManager GetDefault()
		{
			throw new global::System.NotImplementedException("The member AppInstallerManager AppInstallerManager.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInstallerManager%20AppInstallerManager.GetDefault%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Management.Deployment.AppInstallerManager GetForSystem()
		{
			throw new global::System.NotImplementedException("The member AppInstallerManager AppInstallerManager.GetForSystem() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInstallerManager%20AppInstallerManager.GetForSystem%28%29");
		}
		#endif
	}
}
