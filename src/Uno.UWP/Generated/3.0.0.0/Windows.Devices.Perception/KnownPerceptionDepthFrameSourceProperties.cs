#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class KnownPerceptionDepthFrameSourceProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string MaxDepth
		{
			get
			{
				throw new global::System.NotImplementedException("The member string KnownPerceptionDepthFrameSourceProperties.MaxDepth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string MinDepth
		{
			get
			{
				throw new global::System.NotImplementedException("The member string KnownPerceptionDepthFrameSourceProperties.MinDepth is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.KnownPerceptionDepthFrameSourceProperties.MinDepth.get
		// Forced skipping of method Windows.Devices.Perception.KnownPerceptionDepthFrameSourceProperties.MaxDepth.get
	}
}
