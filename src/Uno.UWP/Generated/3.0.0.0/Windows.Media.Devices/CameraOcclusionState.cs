#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CameraOcclusionState 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsOccluded
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CameraOcclusionState.IsOccluded is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.CameraOcclusionState.IsOccluded.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsOcclusionKind( global::Windows.Media.Devices.CameraOcclusionKind occlusionKind)
		{
			throw new global::System.NotImplementedException("The member bool CameraOcclusionState.IsOcclusionKind(CameraOcclusionKind occlusionKind) is not implemented in Uno.");
		}
		#endif
	}
}
