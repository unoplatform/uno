#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaCapture : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.AudioDeviceController AudioDeviceController
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioDeviceController MediaCapture.AudioDeviceController is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.MediaCaptureSettings MediaCaptureSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaCaptureSettings MediaCapture.MediaCaptureSettings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.VideoDeviceController VideoDeviceController
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoDeviceController MediaCapture.VideoDeviceController is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.CameraStreamState CameraStreamState
		{
			get
			{
				throw new global::System.NotImplementedException("The member CameraStreamState MediaCapture.CameraStreamState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.MediaCaptureThermalStatus ThermalStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaCaptureThermalStatus MediaCapture.ThermalStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Media.Capture.Frames.MediaFrameSource> FrameSources
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, MediaFrameSource> MediaCapture.FrameSources is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MediaCapture() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "MediaCapture.MediaCapture()");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCapture.MediaCapture()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction InitializeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.InitializeAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction InitializeAsync( global::Windows.Media.Capture.MediaCaptureInitializationSettings mediaCaptureInitializationSettings)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.InitializeAsync(MediaCaptureInitializationSettings mediaCaptureInitializationSettings) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartRecordToStorageFileAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile encodingProfile, IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartRecordToStreamAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  global::Windows.Storage.Streams.IRandomAccessStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StartRecordToStreamAsync(MediaEncodingProfile encodingProfile, IRandomAccessStream stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartRecordToCustomSinkAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  global::Windows.Media.IMediaExtension customMediaSink)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StartRecordToCustomSinkAsync(MediaEncodingProfile encodingProfile, IMediaExtension customMediaSink) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartRecordToCustomSinkAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  string customSinkActivationId,  global::Windows.Foundation.Collections.IPropertySet customSinkSettings)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StartRecordToCustomSinkAsync(MediaEncodingProfile encodingProfile, string customSinkActivationId, IPropertySet customSinkSettings) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StopRecordAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StopRecordAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CapturePhotoToStorageFileAsync( global::Windows.Media.MediaProperties.ImageEncodingProperties type,  global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties type, IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CapturePhotoToStreamAsync( global::Windows.Media.MediaProperties.ImageEncodingProperties type,  global::Windows.Storage.Streams.IRandomAccessStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties type, IRandomAccessStream stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction AddEffectAsync( global::Windows.Media.Capture.MediaStreamType mediaStreamType,  string effectActivationID,  global::Windows.Foundation.Collections.IPropertySet effectSettings)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.AddEffectAsync(MediaStreamType mediaStreamType, string effectActivationID, IPropertySet effectSettings) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ClearEffectsAsync( global::Windows.Media.Capture.MediaStreamType mediaStreamType)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.ClearEffectsAsync(MediaStreamType mediaStreamType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetEncoderProperty( global::Windows.Media.Capture.MediaStreamType mediaStreamType,  global::System.Guid propertyId,  object propertyValue)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "void MediaCapture.SetEncoderProperty(MediaStreamType mediaStreamType, Guid propertyId, object propertyValue)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object GetEncoderProperty( global::Windows.Media.Capture.MediaStreamType mediaStreamType,  global::System.Guid propertyId)
		{
			throw new global::System.NotImplementedException("The member object MediaCapture.GetEncoderProperty(MediaStreamType mediaStreamType, Guid propertyId) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCapture.Failed.add
		// Forced skipping of method Windows.Media.Capture.MediaCapture.Failed.remove
		// Forced skipping of method Windows.Media.Capture.MediaCapture.RecordLimitationExceeded.add
		// Forced skipping of method Windows.Media.Capture.MediaCapture.RecordLimitationExceeded.remove
		// Forced skipping of method Windows.Media.Capture.MediaCapture.MediaCaptureSettings.get
		// Forced skipping of method Windows.Media.Capture.MediaCapture.AudioDeviceController.get
		// Forced skipping of method Windows.Media.Capture.MediaCapture.VideoDeviceController.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPreviewMirroring( bool value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "void MediaCapture.SetPreviewMirroring(bool value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool GetPreviewMirroring()
		{
			throw new global::System.NotImplementedException("The member bool MediaCapture.GetPreviewMirroring() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPreviewRotation( global::Windows.Media.Capture.VideoRotation value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "void MediaCapture.SetPreviewRotation(VideoRotation value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.VideoRotation GetPreviewRotation()
		{
			throw new global::System.NotImplementedException("The member VideoRotation MediaCapture.GetPreviewRotation() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetRecordRotation( global::Windows.Media.Capture.VideoRotation value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "void MediaCapture.SetRecordRotation(VideoRotation value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.VideoRotation GetRecordRotation()
		{
			throw new global::System.NotImplementedException("The member VideoRotation MediaCapture.GetRecordRotation() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartPreviewAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StartPreviewAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartPreviewToCustomSinkAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  global::Windows.Media.IMediaExtension customMediaSink)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StartPreviewToCustomSinkAsync(MediaEncodingProfile encodingProfile, IMediaExtension customMediaSink) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartPreviewToCustomSinkAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  string customSinkActivationId,  global::Windows.Foundation.Collections.IPropertySet customSinkSettings)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StartPreviewToCustomSinkAsync(MediaEncodingProfile encodingProfile, string customSinkActivationId, IPropertySet customSinkSettings) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StopPreviewAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.StopPreviewAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.LowLagMediaRecording> PrepareLowLagRecordToStorageFileAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LowLagMediaRecording> MediaCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile encodingProfile, IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.LowLagMediaRecording> PrepareLowLagRecordToStreamAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  global::Windows.Storage.Streams.IRandomAccessStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LowLagMediaRecording> MediaCapture.PrepareLowLagRecordToStreamAsync(MediaEncodingProfile encodingProfile, IRandomAccessStream stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.LowLagMediaRecording> PrepareLowLagRecordToCustomSinkAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  global::Windows.Media.IMediaExtension customMediaSink)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LowLagMediaRecording> MediaCapture.PrepareLowLagRecordToCustomSinkAsync(MediaEncodingProfile encodingProfile, IMediaExtension customMediaSink) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.LowLagMediaRecording> PrepareLowLagRecordToCustomSinkAsync( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile,  string customSinkActivationId,  global::Windows.Foundation.Collections.IPropertySet customSinkSettings)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LowLagMediaRecording> MediaCapture.PrepareLowLagRecordToCustomSinkAsync(MediaEncodingProfile encodingProfile, string customSinkActivationId, IPropertySet customSinkSettings) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.LowLagPhotoCapture> PrepareLowLagPhotoCaptureAsync( global::Windows.Media.MediaProperties.ImageEncodingProperties type)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LowLagPhotoCapture> MediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties type) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.LowLagPhotoSequenceCapture> PrepareLowLagPhotoSequenceCaptureAsync( global::Windows.Media.MediaProperties.ImageEncodingProperties type)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<LowLagPhotoSequenceCapture> MediaCapture.PrepareLowLagPhotoSequenceCaptureAsync(ImageEncodingProperties type) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetEncodingPropertiesAsync( global::Windows.Media.Capture.MediaStreamType mediaStreamType,  global::Windows.Media.MediaProperties.IMediaEncodingProperties mediaEncodingProperties,  global::Windows.Media.MediaProperties.MediaPropertySet encoderProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.SetEncodingPropertiesAsync(MediaStreamType mediaStreamType, IMediaEncodingProperties mediaEncodingProperties, MediaPropertySet encoderProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "void MediaCapture.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Core.VariablePhotoSequenceCapture> PrepareVariablePhotoSequenceCaptureAsync( global::Windows.Media.MediaProperties.ImageEncodingProperties type)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VariablePhotoSequenceCapture> MediaCapture.PrepareVariablePhotoSequenceCaptureAsync(ImageEncodingProperties type) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCapture.FocusChanged.add
		// Forced skipping of method Windows.Media.Capture.MediaCapture.FocusChanged.remove
		// Forced skipping of method Windows.Media.Capture.MediaCapture.PhotoConfirmationCaptured.add
		// Forced skipping of method Windows.Media.Capture.MediaCapture.PhotoConfirmationCaptured.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.IMediaExtension> AddAudioEffectAsync( global::Windows.Media.Effects.IAudioEffectDefinition definition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IMediaExtension> MediaCapture.AddAudioEffectAsync(IAudioEffectDefinition definition) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.IMediaExtension> AddVideoEffectAsync( global::Windows.Media.Effects.IVideoEffectDefinition definition,  global::Windows.Media.Capture.MediaStreamType mediaStreamType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IMediaExtension> MediaCapture.AddVideoEffectAsync(IVideoEffectDefinition definition, MediaStreamType mediaStreamType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction PauseRecordAsync( global::Windows.Media.Devices.MediaCapturePauseBehavior behavior)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.PauseRecordAsync(MediaCapturePauseBehavior behavior) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ResumeRecordAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.ResumeRecordAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCapture.CameraStreamStateChanged.add
		// Forced skipping of method Windows.Media.Capture.MediaCapture.CameraStreamStateChanged.remove
		// Forced skipping of method Windows.Media.Capture.MediaCapture.CameraStreamState.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.VideoFrame> GetPreviewFrameAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VideoFrame> MediaCapture.GetPreviewFrameAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.VideoFrame> GetPreviewFrameAsync( global::Windows.Media.VideoFrame destination)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VideoFrame> MediaCapture.GetPreviewFrameAsync(VideoFrame destination) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCapture.ThermalStatusChanged.add
		// Forced skipping of method Windows.Media.Capture.MediaCapture.ThermalStatusChanged.remove
		// Forced skipping of method Windows.Media.Capture.MediaCapture.ThermalStatus.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.AdvancedPhotoCapture> PrepareAdvancedPhotoCaptureAsync( global::Windows.Media.MediaProperties.ImageEncodingProperties encodingProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AdvancedPhotoCapture> MediaCapture.PrepareAdvancedPhotoCaptureAsync(ImageEncodingProperties encodingProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RemoveEffectAsync( global::Windows.Media.IMediaExtension effect)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaCapture.RemoveEffectAsync(IMediaExtension effect) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.MediaCapturePauseResult> PauseRecordWithResultAsync( global::Windows.Media.Devices.MediaCapturePauseBehavior behavior)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaCapturePauseResult> MediaCapture.PauseRecordWithResultAsync(MediaCapturePauseBehavior behavior) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.MediaCaptureStopResult> StopRecordWithResultAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaCaptureStopResult> MediaCapture.StopRecordWithResultAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCapture.FrameSources.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MediaFrameReader> CreateFrameReaderAsync( global::Windows.Media.Capture.Frames.MediaFrameSource inputSource)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaFrameReader> MediaCapture.CreateFrameReaderAsync(MediaFrameSource inputSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MediaFrameReader> CreateFrameReaderAsync( global::Windows.Media.Capture.Frames.MediaFrameSource inputSource,  string outputSubtype)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaFrameReader> MediaCapture.CreateFrameReaderAsync(MediaFrameSource inputSource, string outputSubtype) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MediaFrameReader> CreateFrameReaderAsync( global::Windows.Media.Capture.Frames.MediaFrameSource inputSource,  string outputSubtype,  global::Windows.Graphics.Imaging.BitmapSize outputSize)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaFrameReader> MediaCapture.CreateFrameReaderAsync(MediaFrameSource inputSource, string outputSubtype, BitmapSize outputSize) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCapture.CaptureDeviceExclusiveControlStatusChanged.add
		// Forced skipping of method Windows.Media.Capture.MediaCapture.CaptureDeviceExclusiveControlStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MultiSourceMediaFrameReader> CreateMultiSourceFrameReaderAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Media.Capture.Frames.MediaFrameSource> inputSources)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MultiSourceMediaFrameReader> MediaCapture.CreateMultiSourceFrameReaderAsync(IEnumerable<MediaFrameSource> inputSources) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.MediaCaptureRelativePanelWatcher CreateRelativePanelWatcher( global::Windows.Media.Capture.StreamingCaptureMode captureMode,  global::Windows.UI.WindowManagement.DisplayRegion displayRegion)
		{
			throw new global::System.NotImplementedException("The member MediaCaptureRelativePanelWatcher MediaCapture.CreateRelativePanelWatcher(StreamingCaptureMode captureMode, DisplayRegion displayRegion) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsVideoProfileSupported( string videoDeviceId)
		{
			throw new global::System.NotImplementedException("The member bool MediaCapture.IsVideoProfileSupported(string videoDeviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Capture.MediaCaptureVideoProfile> FindAllVideoProfiles( string videoDeviceId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<MediaCaptureVideoProfile> MediaCapture.FindAllVideoProfiles(string videoDeviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Capture.MediaCaptureVideoProfile> FindConcurrentProfiles( string videoDeviceId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<MediaCaptureVideoProfile> MediaCapture.FindConcurrentProfiles(string videoDeviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Capture.MediaCaptureVideoProfile> FindKnownVideoProfiles( string videoDeviceId,  global::Windows.Media.Capture.KnownVideoProfile name)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<MediaCaptureVideoProfile> MediaCapture.FindKnownVideoProfiles(string videoDeviceId, KnownVideoProfile name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Media.Capture.MediaCaptureFailedEventHandler Failed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event MediaCaptureFailedEventHandler MediaCapture.Failed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event MediaCaptureFailedEventHandler MediaCapture.Failed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Media.Capture.RecordLimitationExceededEventHandler RecordLimitationExceeded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event RecordLimitationExceededEventHandler MediaCapture.RecordLimitationExceeded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event RecordLimitationExceededEventHandler MediaCapture.RecordLimitationExceeded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.MediaCapture, global::Windows.Media.Capture.MediaCaptureFocusChangedEventArgs> FocusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, MediaCaptureFocusChangedEventArgs> MediaCapture.FocusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, MediaCaptureFocusChangedEventArgs> MediaCapture.FocusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.MediaCapture, global::Windows.Media.Capture.PhotoConfirmationCapturedEventArgs> PhotoConfirmationCaptured
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, PhotoConfirmationCapturedEventArgs> MediaCapture.PhotoConfirmationCaptured");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, PhotoConfirmationCapturedEventArgs> MediaCapture.PhotoConfirmationCaptured");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.MediaCapture, object> CameraStreamStateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, object> MediaCapture.CameraStreamStateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, object> MediaCapture.CameraStreamStateChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.MediaCapture, object> ThermalStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, object> MediaCapture.ThermalStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, object> MediaCapture.ThermalStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.MediaCapture, global::Windows.Media.Capture.MediaCaptureDeviceExclusiveControlStatusChangedEventArgs> CaptureDeviceExclusiveControlStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs> MediaCapture.CaptureDeviceExclusiveControlStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.MediaCapture", "event TypedEventHandler<MediaCapture, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs> MediaCapture.CaptureDeviceExclusiveControlStatusChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
