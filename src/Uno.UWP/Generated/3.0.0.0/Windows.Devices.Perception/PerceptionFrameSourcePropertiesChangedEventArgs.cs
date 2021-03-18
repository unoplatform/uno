#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionFrameSourcePropertiesChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.CollectionChange CollectionChange
		{
			get
			{
				throw new global::System.NotImplementedException("The member CollectionChange PerceptionFrameSourcePropertiesChangedEventArgs.CollectionChange is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Key
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PerceptionFrameSourcePropertiesChangedEventArgs.Key is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.PerceptionFrameSourcePropertiesChangedEventArgs.CollectionChange.get
		// Forced skipping of method Windows.Devices.Perception.PerceptionFrameSourcePropertiesChangedEventArgs.Key.get
	}
}
