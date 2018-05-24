#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechSynthesis
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VoiceGender 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Male,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Female,
		#endif
	}
	#endif
}
