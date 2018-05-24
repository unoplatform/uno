#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAudioNode : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool ConsumeInput
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.Collections.Generic.IList<global::Windows.Media.Effects.IAudioEffectDefinition> EffectDefinitions
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Media.MediaProperties.AudioEncodingProperties EncodingProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		double OutgoingGain
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.IAudioNode.EffectDefinitions.get
		// Forced skipping of method Windows.Media.Audio.IAudioNode.OutgoingGain.set
		// Forced skipping of method Windows.Media.Audio.IAudioNode.OutgoingGain.get
		// Forced skipping of method Windows.Media.Audio.IAudioNode.EncodingProperties.get
		// Forced skipping of method Windows.Media.Audio.IAudioNode.ConsumeInput.get
		// Forced skipping of method Windows.Media.Audio.IAudioNode.ConsumeInput.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Start();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Stop();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Reset();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void DisableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void EnableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition);
		#endif
	}
}
