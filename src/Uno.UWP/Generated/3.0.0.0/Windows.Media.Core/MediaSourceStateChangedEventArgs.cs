#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaSourceStateChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaSourceState NewState
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSourceState MediaSourceStateChangedEventArgs.NewState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaSourceState OldState
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSourceState MediaSourceStateChangedEventArgs.OldState is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaSourceStateChangedEventArgs.OldState.get
		// Forced skipping of method Windows.Media.Core.MediaSourceStateChangedEventArgs.NewState.get
	}
}
