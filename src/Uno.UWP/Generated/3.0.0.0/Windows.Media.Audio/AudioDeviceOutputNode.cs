#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioDeviceOutputNode : global::Windows.Media.Audio.IAudioNode,global::System.IDisposable,global::Windows.Media.Audio.IAudioNodeWithListener
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformation Device
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation AudioDeviceOutputNode.Device is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double OutgoingGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioDeviceOutputNode.OutgoingGain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "double AudioDeviceOutputNode.OutgoingGain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ConsumeInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AudioDeviceOutputNode.ConsumeInput is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "bool AudioDeviceOutputNode.ConsumeInput");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Effects.IAudioEffectDefinition> EffectDefinitions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<IAudioEffectDefinition> AudioDeviceOutputNode.EffectDefinitions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.AudioEncodingProperties EncodingProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioEncodingProperties AudioDeviceOutputNode.EncodingProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioNodeListener Listener
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioNodeListener AudioDeviceOutputNode.Listener is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "AudioNodeListener AudioDeviceOutputNode.Listener");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.Device.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.EffectDefinitions.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.OutgoingGain.set
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.OutgoingGain.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.EncodingProperties.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.ConsumeInput.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.ConsumeInput.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "void AudioDeviceOutputNode.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "void AudioDeviceOutputNode.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reset()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "void AudioDeviceOutputNode.Reset()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DisableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "void AudioDeviceOutputNode.DisableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "void AudioDeviceOutputNode.EnableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceOutputNode", "void AudioDeviceOutputNode.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.Listener.set
		// Forced skipping of method Windows.Media.Audio.AudioDeviceOutputNode.Listener.get
		// Processing: Windows.Media.Audio.IAudioNode
		// Processing: System.IDisposable
		// Processing: Windows.Media.Audio.IAudioNodeWithListener
	}
}
