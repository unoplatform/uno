#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioFileOutputNode : global::Windows.Media.Audio.IAudioNode,global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.IStorageFile File
		{
			get
			{
				throw new global::System.NotImplementedException("The member IStorageFile AudioFileOutputNode.File is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaEncodingProfile FileEncodingProfile
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaEncodingProfile AudioFileOutputNode.FileEncodingProfile is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double OutgoingGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioFileOutputNode.OutgoingGain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "double AudioFileOutputNode.OutgoingGain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ConsumeInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AudioFileOutputNode.ConsumeInput is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "bool AudioFileOutputNode.ConsumeInput");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Effects.IAudioEffectDefinition> EffectDefinitions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<IAudioEffectDefinition> AudioFileOutputNode.EffectDefinitions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.AudioEncodingProperties EncodingProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioEncodingProperties AudioFileOutputNode.EncodingProperties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.File.get
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.FileEncodingProfile.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Transcoding.TranscodeFailureReason> FinalizeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<TranscodeFailureReason> AudioFileOutputNode.FinalizeAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.EffectDefinitions.get
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.OutgoingGain.set
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.OutgoingGain.get
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.EncodingProperties.get
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.ConsumeInput.get
		// Forced skipping of method Windows.Media.Audio.AudioFileOutputNode.ConsumeInput.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "void AudioFileOutputNode.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "void AudioFileOutputNode.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reset()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "void AudioFileOutputNode.Reset()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DisableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "void AudioFileOutputNode.DisableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "void AudioFileOutputNode.EnableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioFileOutputNode", "void AudioFileOutputNode.Dispose()");
		}
		#endif
		// Processing: Windows.Media.Audio.IAudioNode
		// Processing: System.IDisposable
	}
}
