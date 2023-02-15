#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionInfraredFrameArrivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan RelativeTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan PerceptionInfraredFrameArrivedEventArgs.RelativeTime is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20PerceptionInfraredFrameArrivedEventArgs.RelativeTime");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.PerceptionInfraredFrameArrivedEventArgs.RelativeTime.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.PerceptionInfraredFrame TryOpenFrame()
		{
			throw new global::System.NotImplementedException("The member PerceptionInfraredFrame PerceptionInfraredFrameArrivedEventArgs.TryOpenFrame() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PerceptionInfraredFrame%20PerceptionInfraredFrameArrivedEventArgs.TryOpenFrame%28%29");
		}
		#endif
	}
}
