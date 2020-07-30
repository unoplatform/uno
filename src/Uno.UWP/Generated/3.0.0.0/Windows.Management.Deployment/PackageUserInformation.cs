#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageUserInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Management.Deployment.PackageInstallState InstallState
		{
			get
			{
				throw new global::System.NotImplementedException("The member PackageInstallState PackageUserInformation.InstallState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string UserSecurityId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PackageUserInformation.UserSecurityId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Management.Deployment.PackageUserInformation.UserSecurityId.get
		// Forced skipping of method Windows.Management.Deployment.PackageUserInformation.InstallState.get
	}
}
