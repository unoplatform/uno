#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppDiagnosticInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.AppInfo AppInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppInfo AppDiagnosticInfo.AppInfo is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.AppDiagnosticInfo.AppInfo.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.System.AppResourceGroupInfo> GetResourceGroups()
		{
			throw new global::System.NotImplementedException("The member IList<AppResourceGroupInfo> AppDiagnosticInfo.GetResourceGroups() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.AppResourceGroupInfoWatcher CreateResourceGroupWatcher()
		{
			throw new global::System.NotImplementedException("The member AppResourceGroupInfoWatcher AppDiagnosticInfo.CreateResourceGroupWatcher() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.System.AppActivationResult> LaunchAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppActivationResult> AppDiagnosticInfo.LaunchAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.AppDiagnosticInfoWatcher CreateWatcher()
		{
			throw new global::System.NotImplementedException("The member AppDiagnosticInfoWatcher AppDiagnosticInfo.CreateWatcher() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.System.DiagnosticAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DiagnosticAccessStatus> AppDiagnosticInfo.RequestAccessAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.System.AppDiagnosticInfo>> RequestInfoForPackageAsync( string packageFamilyName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<AppDiagnosticInfo>> AppDiagnosticInfo.RequestInfoForPackageAsync(string packageFamilyName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.System.AppDiagnosticInfo>> RequestInfoForAppAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<AppDiagnosticInfo>> AppDiagnosticInfo.RequestInfoForAppAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.System.AppDiagnosticInfo>> RequestInfoForAppAsync( string appUserModelId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<AppDiagnosticInfo>> AppDiagnosticInfo.RequestInfoForAppAsync(string appUserModelId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.System.AppDiagnosticInfo>> RequestInfoAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<AppDiagnosticInfo>> AppDiagnosticInfo.RequestInfoAsync() is not implemented in Uno.");
		}
		#endif
	}
}
