#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioGraphConnection 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Gain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioGraphConnection.Gain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraphConnection", "double AudioGraphConnection.Gain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.IAudioNode Destination
		{
			get
			{
				throw new global::System.NotImplementedException("The member IAudioNode AudioGraphConnection.Destination is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioGraphConnection.Destination.get
		// Forced skipping of method Windows.Media.Audio.AudioGraphConnection.Gain.set
		// Forced skipping of method Windows.Media.Audio.AudioGraphConnection.Gain.get
	}
}
