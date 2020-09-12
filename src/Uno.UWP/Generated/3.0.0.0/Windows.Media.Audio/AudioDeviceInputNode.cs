#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioDeviceInputNode : global::Windows.Media.Audio.IAudioInputNode,global::Windows.Media.Audio.IAudioNode,global::System.IDisposable,global::Windows.Media.Audio.IAudioInputNode2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformation Device
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation AudioDeviceInputNode.Device is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Audio.AudioGraphConnection> OutgoingConnections
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<AudioGraphConnection> AudioDeviceInputNode.OutgoingConnections is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioNodeEmitter Emitter
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioNodeEmitter AudioDeviceInputNode.Emitter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double OutgoingGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioDeviceInputNode.OutgoingGain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "double AudioDeviceInputNode.OutgoingGain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ConsumeInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AudioDeviceInputNode.ConsumeInput is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "bool AudioDeviceInputNode.ConsumeInput");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Effects.IAudioEffectDefinition> EffectDefinitions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<IAudioEffectDefinition> AudioDeviceInputNode.EffectDefinitions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.AudioEncodingProperties EncodingProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioEncodingProperties AudioDeviceInputNode.EncodingProperties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.Device.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.OutgoingConnections.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOutgoingConnection( global::Windows.Media.Audio.IAudioNode destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.AddOutgoingConnection(IAudioNode destination)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOutgoingConnection( global::Windows.Media.Audio.IAudioNode destination,  double gain)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.AddOutgoingConnection(IAudioNode destination, double gain)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveOutgoingConnection( global::Windows.Media.Audio.IAudioNode destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.RemoveOutgoingConnection(IAudioNode destination)");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.EffectDefinitions.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.OutgoingGain.set
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.OutgoingGain.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.EncodingProperties.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.ConsumeInput.get
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.ConsumeInput.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reset()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.Reset()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DisableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.DisableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.EnableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioDeviceInputNode", "void AudioDeviceInputNode.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioDeviceInputNode.Emitter.get
		// Processing: Windows.Media.Audio.IAudioInputNode
		// Processing: Windows.Media.Audio.IAudioNode
		// Processing: System.IDisposable
		// Processing: Windows.Media.Audio.IAudioInputNode2
	}
}
