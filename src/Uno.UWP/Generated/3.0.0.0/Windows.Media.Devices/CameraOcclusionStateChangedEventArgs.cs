#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CameraOcclusionStateChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.CameraOcclusionState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member CameraOcclusionState CameraOcclusionStateChangedEventArgs.State is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CameraOcclusionState%20CameraOcclusionStateChangedEventArgs.State");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.CameraOcclusionStateChangedEventArgs.State.get
	}
}
