#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SoundLevelBroker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.SoundLevel SoundLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member SoundLevel SoundLevelBroker.SoundLevel is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.Preview.SoundLevelBroker.SoundLevel.get
		// Forced skipping of method Windows.Media.Core.Preview.SoundLevelBroker.SoundLevelChanged.add
		// Forced skipping of method Windows.Media.Core.Preview.SoundLevelBroker.SoundLevelChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> SoundLevelChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.Preview.SoundLevelBroker", "event EventHandler<object> SoundLevelBroker.SoundLevelChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.Preview.SoundLevelBroker", "event EventHandler<object> SoundLevelBroker.SoundLevelChanged");
			}
		}
		#endif
	}
}
