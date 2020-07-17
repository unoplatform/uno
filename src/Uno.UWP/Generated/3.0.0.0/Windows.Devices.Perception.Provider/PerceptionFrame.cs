#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionFrame 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan RelativeTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan PerceptionFrame.RelativeTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.Provider.PerceptionFrame", "TimeSpan PerceptionFrame.RelativeTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IMemoryBuffer FrameData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMemoryBuffer PerceptionFrame.FrameData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.ValueSet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet PerceptionFrame.Properties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionFrame.RelativeTime.get
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionFrame.RelativeTime.set
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionFrame.Properties.get
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionFrame.FrameData.get
	}
}
