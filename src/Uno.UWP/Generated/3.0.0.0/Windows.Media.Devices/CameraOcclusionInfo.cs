#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CameraOcclusionInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.CameraOcclusionState GetState()
		{
			throw new global::System.NotImplementedException("The member CameraOcclusionState CameraOcclusionInfo.GetState() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CameraOcclusionState%20CameraOcclusionInfo.GetState%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsOcclusionKindSupported( global::Windows.Media.Devices.CameraOcclusionKind occlusionKind)
		{
			throw new global::System.NotImplementedException("The member bool CameraOcclusionInfo.IsOcclusionKindSupported(CameraOcclusionKind occlusionKind) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CameraOcclusionInfo.IsOcclusionKindSupported%28CameraOcclusionKind%20occlusionKind%29");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.CameraOcclusionInfo.StateChanged.add
		// Forced skipping of method Windows.Media.Devices.CameraOcclusionInfo.StateChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Devices.CameraOcclusionInfo, global::Windows.Media.Devices.CameraOcclusionStateChangedEventArgs> StateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.CameraOcclusionInfo", "event TypedEventHandler<CameraOcclusionInfo, CameraOcclusionStateChangedEventArgs> CameraOcclusionInfo.StateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.CameraOcclusionInfo", "event TypedEventHandler<CameraOcclusionInfo, CameraOcclusionStateChangedEventArgs> CameraOcclusionInfo.StateChanged");
			}
		}
		#endif
	}
}
