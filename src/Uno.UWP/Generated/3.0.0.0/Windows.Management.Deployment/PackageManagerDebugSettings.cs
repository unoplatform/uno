#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageManagerDebugSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetContentGroupStateAsync( global::Windows.ApplicationModel.Package package,  string contentGroupName,  global::Windows.ApplicationModel.PackageContentGroupState state)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PackageManagerDebugSettings.SetContentGroupStateAsync(Package package, string contentGroupName, PackageContentGroupState state) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetContentGroupStateAsync( global::Windows.ApplicationModel.Package package,  string contentGroupName,  global::Windows.ApplicationModel.PackageContentGroupState state,  double completionPercentage)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PackageManagerDebugSettings.SetContentGroupStateAsync(Package package, string contentGroupName, PackageContentGroupState state, double completionPercentage) is not implemented in Uno.");
		}
		#endif
	}
}
