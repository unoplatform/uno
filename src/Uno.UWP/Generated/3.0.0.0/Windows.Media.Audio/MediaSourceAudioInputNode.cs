#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaSourceAudioInputNode : global::Windows.Media.Audio.IAudioInputNode2,global::Windows.Media.Audio.IAudioInputNode,global::Windows.Media.Audio.IAudioNode,global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Audio.AudioGraphConnection> OutgoingConnections
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<AudioGraphConnection> MediaSourceAudioInputNode.OutgoingConnections is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioNodeEmitter Emitter
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioNodeEmitter MediaSourceAudioInputNode.Emitter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double OutgoingGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double MediaSourceAudioInputNode.OutgoingGain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "double MediaSourceAudioInputNode.OutgoingGain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ConsumeInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaSourceAudioInputNode.ConsumeInput is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "bool MediaSourceAudioInputNode.ConsumeInput");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Effects.IAudioEffectDefinition> EffectDefinitions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<IAudioEffectDefinition> MediaSourceAudioInputNode.EffectDefinitions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.AudioEncodingProperties EncodingProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioEncodingProperties MediaSourceAudioInputNode.EncodingProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? StartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MediaSourceAudioInputNode.StartTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "TimeSpan? MediaSourceAudioInputNode.StartTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double PlaybackSpeedFactor
		{
			get
			{
				throw new global::System.NotImplementedException("The member double MediaSourceAudioInputNode.PlaybackSpeedFactor is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "double MediaSourceAudioInputNode.PlaybackSpeedFactor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int? LoopCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member int? MediaSourceAudioInputNode.LoopCount is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "int? MediaSourceAudioInputNode.LoopCount");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? EndTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MediaSourceAudioInputNode.EndTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "TimeSpan? MediaSourceAudioInputNode.EndTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MediaSourceAudioInputNode.Duration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaSource MediaSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSource MediaSourceAudioInputNode.MediaSource is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MediaSourceAudioInputNode.Position is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.PlaybackSpeedFactor.set
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.PlaybackSpeedFactor.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.Position.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Seek( global::System.TimeSpan position)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.Seek(TimeSpan position)");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.StartTime.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.StartTime.set
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.EndTime.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.EndTime.set
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.LoopCount.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.LoopCount.set
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.Duration.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.MediaSource.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.MediaSourceCompleted.add
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.MediaSourceCompleted.remove
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.OutgoingConnections.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOutgoingConnection( global::Windows.Media.Audio.IAudioNode destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.AddOutgoingConnection(IAudioNode destination)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOutgoingConnection( global::Windows.Media.Audio.IAudioNode destination,  double gain)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.AddOutgoingConnection(IAudioNode destination, double gain)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveOutgoingConnection( global::Windows.Media.Audio.IAudioNode destination)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.RemoveOutgoingConnection(IAudioNode destination)");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.EffectDefinitions.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.OutgoingGain.set
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.OutgoingGain.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.EncodingProperties.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.ConsumeInput.get
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.ConsumeInput.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reset()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.Reset()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DisableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.DisableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableEffectsByDefinition( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.EnableEffectsByDefinition(IAudioEffectDefinition definition)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "void MediaSourceAudioInputNode.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.MediaSourceAudioInputNode.Emitter.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Audio.MediaSourceAudioInputNode, object> MediaSourceCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "event TypedEventHandler<MediaSourceAudioInputNode, object> MediaSourceAudioInputNode.MediaSourceCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.MediaSourceAudioInputNode", "event TypedEventHandler<MediaSourceAudioInputNode, object> MediaSourceAudioInputNode.MediaSourceCompleted");
			}
		}
		#endif
		// Processing: Windows.Media.Audio.IAudioInputNode2
		// Processing: Windows.Media.Audio.IAudioNode
		// Processing: System.IDisposable
		// Processing: Windows.Media.Audio.IAudioInputNode
	}
}
