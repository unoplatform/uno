#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Cortana
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CortanaPermissionsManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool CortanaPermissionsManager.IsSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ArePermissionsGrantedAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Cortana.CortanaPermission> permissions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> CortanaPermissionsManager.ArePermissionsGrantedAsync(IEnumerable<CortanaPermission> permissions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Cortana.CortanaPermissionsChangeResult> GrantPermissionsAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Cortana.CortanaPermission> permissions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CortanaPermissionsChangeResult> CortanaPermissionsManager.GrantPermissionsAsync(IEnumerable<CortanaPermission> permissions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Cortana.CortanaPermissionsChangeResult> RevokePermissionsAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Cortana.CortanaPermission> permissions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CortanaPermissionsChangeResult> CortanaPermissionsManager.RevokePermissionsAsync(IEnumerable<CortanaPermission> permissions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Cortana.CortanaPermissionsManager GetDefault()
		{
			throw new global::System.NotImplementedException("The member CortanaPermissionsManager CortanaPermissionsManager.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
