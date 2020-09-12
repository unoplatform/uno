#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteLauncher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.System.RemoteLaunchUriStatus> LaunchUriAsync( global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest remoteSystemConnectionRequest,  global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RemoteLaunchUriStatus> RemoteLauncher.LaunchUriAsync(RemoteSystemConnectionRequest remoteSystemConnectionRequest, Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.System.RemoteLaunchUriStatus> LaunchUriAsync( global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest remoteSystemConnectionRequest,  global::System.Uri uri,  global::Windows.System.RemoteLauncherOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RemoteLaunchUriStatus> RemoteLauncher.LaunchUriAsync(RemoteSystemConnectionRequest remoteSystemConnectionRequest, Uri uri, RemoteLauncherOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.System.RemoteLaunchUriStatus> LaunchUriAsync( global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest remoteSystemConnectionRequest,  global::System.Uri uri,  global::Windows.System.RemoteLauncherOptions options,  global::Windows.Foundation.Collections.ValueSet inputData)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RemoteLaunchUriStatus> RemoteLauncher.LaunchUriAsync(RemoteSystemConnectionRequest remoteSystemConnectionRequest, Uri uri, RemoteLauncherOptions options, ValueSet inputData) is not implemented in Uno.");
		}
		#endif
	}
}
