#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioGraph : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong CompletedQuantumCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AudioGraph.CompletedQuantumCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.AudioEncodingProperties EncodingProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioEncodingProperties AudioGraph.EncodingProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int LatencyInSamples
		{
			get
			{
				throw new global::System.NotImplementedException("The member int AudioGraph.LatencyInSamples is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformation PrimaryRenderDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation AudioGraph.PrimaryRenderDevice is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.AudioProcessing RenderDeviceAudioProcessing
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioProcessing AudioGraph.RenderDeviceAudioProcessing is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int SamplesPerQuantum
		{
			get
			{
				throw new global::System.NotImplementedException("The member int AudioGraph.SamplesPerQuantum is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioFrameInputNode CreateFrameInputNode()
		{
			throw new global::System.NotImplementedException("The member AudioFrameInputNode AudioGraph.CreateFrameInputNode() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioFrameInputNode CreateFrameInputNode( global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties)
		{
			throw new global::System.NotImplementedException("The member AudioFrameInputNode AudioGraph.CreateFrameInputNode(AudioEncodingProperties encodingProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioDeviceInputNodeResult> CreateDeviceInputNodeAsync( global::Windows.Media.Capture.MediaCategory category)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioDeviceInputNodeResult> AudioGraph.CreateDeviceInputNodeAsync(MediaCategory category) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioDeviceInputNodeResult> CreateDeviceInputNodeAsync( global::Windows.Media.Capture.MediaCategory category,  global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioDeviceInputNodeResult> AudioGraph.CreateDeviceInputNodeAsync(MediaCategory category, AudioEncodingProperties encodingProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioDeviceInputNodeResult> CreateDeviceInputNodeAsync( global::Windows.Media.Capture.MediaCategory category,  global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties,  global::Windows.Devices.Enumeration.DeviceInformation device)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioDeviceInputNodeResult> AudioGraph.CreateDeviceInputNodeAsync(MediaCategory category, AudioEncodingProperties encodingProperties, DeviceInformation device) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioFrameOutputNode CreateFrameOutputNode()
		{
			throw new global::System.NotImplementedException("The member AudioFrameOutputNode AudioGraph.CreateFrameOutputNode() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioFrameOutputNode CreateFrameOutputNode( global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties)
		{
			throw new global::System.NotImplementedException("The member AudioFrameOutputNode AudioGraph.CreateFrameOutputNode(AudioEncodingProperties encodingProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioDeviceOutputNodeResult> CreateDeviceOutputNodeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioDeviceOutputNodeResult> AudioGraph.CreateDeviceOutputNodeAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioFileInputNodeResult> CreateFileInputNodeAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioFileInputNodeResult> AudioGraph.CreateFileInputNodeAsync(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioFileOutputNodeResult> CreateFileOutputNodeAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioFileOutputNodeResult> AudioGraph.CreateFileOutputNodeAsync(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioFileOutputNodeResult> CreateFileOutputNodeAsync( global::Windows.Storage.IStorageFile file,  global::Windows.Media.MediaProperties.MediaEncodingProfile fileEncodingProfile)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioFileOutputNodeResult> AudioGraph.CreateFileOutputNodeAsync(IStorageFile file, MediaEncodingProfile fileEncodingProfile) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioSubmixNode CreateSubmixNode()
		{
			throw new global::System.NotImplementedException("The member AudioSubmixNode AudioGraph.CreateSubmixNode() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioSubmixNode CreateSubmixNode( global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties)
		{
			throw new global::System.NotImplementedException("The member AudioSubmixNode AudioGraph.CreateSubmixNode(AudioEncodingProperties encodingProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "void AudioGraph.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "void AudioGraph.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ResetAllNodes()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "void AudioGraph.ResetAllNodes()");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioGraph.QuantumStarted.add
		// Forced skipping of method Windows.Media.Audio.AudioGraph.QuantumStarted.remove
		// Forced skipping of method Windows.Media.Audio.AudioGraph.QuantumProcessed.add
		// Forced skipping of method Windows.Media.Audio.AudioGraph.QuantumProcessed.remove
		// Forced skipping of method Windows.Media.Audio.AudioGraph.UnrecoverableErrorOccurred.add
		// Forced skipping of method Windows.Media.Audio.AudioGraph.UnrecoverableErrorOccurred.remove
		// Forced skipping of method Windows.Media.Audio.AudioGraph.CompletedQuantumCount.get
		// Forced skipping of method Windows.Media.Audio.AudioGraph.EncodingProperties.get
		// Forced skipping of method Windows.Media.Audio.AudioGraph.LatencyInSamples.get
		// Forced skipping of method Windows.Media.Audio.AudioGraph.PrimaryRenderDevice.get
		// Forced skipping of method Windows.Media.Audio.AudioGraph.RenderDeviceAudioProcessing.get
		// Forced skipping of method Windows.Media.Audio.AudioGraph.SamplesPerQuantum.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "void AudioGraph.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioFrameInputNode CreateFrameInputNode( global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties,  global::Windows.Media.Audio.AudioNodeEmitter emitter)
		{
			throw new global::System.NotImplementedException("The member AudioFrameInputNode AudioGraph.CreateFrameInputNode(AudioEncodingProperties encodingProperties, AudioNodeEmitter emitter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioDeviceInputNodeResult> CreateDeviceInputNodeAsync( global::Windows.Media.Capture.MediaCategory category,  global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties,  global::Windows.Devices.Enumeration.DeviceInformation device,  global::Windows.Media.Audio.AudioNodeEmitter emitter)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioDeviceInputNodeResult> AudioGraph.CreateDeviceInputNodeAsync(MediaCategory category, AudioEncodingProperties encodingProperties, DeviceInformation device, AudioNodeEmitter emitter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioFileInputNodeResult> CreateFileInputNodeAsync( global::Windows.Storage.IStorageFile file,  global::Windows.Media.Audio.AudioNodeEmitter emitter)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioFileInputNodeResult> AudioGraph.CreateFileInputNodeAsync(IStorageFile file, AudioNodeEmitter emitter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioSubmixNode CreateSubmixNode( global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties,  global::Windows.Media.Audio.AudioNodeEmitter emitter)
		{
			throw new global::System.NotImplementedException("The member AudioSubmixNode AudioGraph.CreateSubmixNode(AudioEncodingProperties encodingProperties, AudioNodeEmitter emitter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioGraphBatchUpdater CreateBatchUpdater()
		{
			throw new global::System.NotImplementedException("The member AudioGraphBatchUpdater AudioGraph.CreateBatchUpdater() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateMediaSourceAudioInputNodeResult> CreateMediaSourceAudioInputNodeAsync( global::Windows.Media.Core.MediaSource mediaSource)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateMediaSourceAudioInputNodeResult> AudioGraph.CreateMediaSourceAudioInputNodeAsync(MediaSource mediaSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateMediaSourceAudioInputNodeResult> CreateMediaSourceAudioInputNodeAsync( global::Windows.Media.Core.MediaSource mediaSource,  global::Windows.Media.Audio.AudioNodeEmitter emitter)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateMediaSourceAudioInputNodeResult> AudioGraph.CreateMediaSourceAudioInputNodeAsync(MediaSource mediaSource, AudioNodeEmitter emitter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.CreateAudioGraphResult> CreateAsync( global::Windows.Media.Audio.AudioGraphSettings settings)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CreateAudioGraphResult> AudioGraph.CreateAsync(AudioGraphSettings settings) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Audio.AudioGraph, object> QuantumProcessed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "event TypedEventHandler<AudioGraph, object> AudioGraph.QuantumProcessed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "event TypedEventHandler<AudioGraph, object> AudioGraph.QuantumProcessed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Audio.AudioGraph, object> QuantumStarted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "event TypedEventHandler<AudioGraph, object> AudioGraph.QuantumStarted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "event TypedEventHandler<AudioGraph, object> AudioGraph.QuantumStarted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Audio.AudioGraph, global::Windows.Media.Audio.AudioGraphUnrecoverableErrorOccurredEventArgs> UnrecoverableErrorOccurred
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "event TypedEventHandler<AudioGraph, AudioGraphUnrecoverableErrorOccurredEventArgs> AudioGraph.UnrecoverableErrorOccurred");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioGraph", "event TypedEventHandler<AudioGraph, AudioGraphUnrecoverableErrorOccurredEventArgs> AudioGraph.UnrecoverableErrorOccurred");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
