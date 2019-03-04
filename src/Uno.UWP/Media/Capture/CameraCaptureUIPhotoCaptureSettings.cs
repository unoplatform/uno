#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	public  partial class CameraCaptureUIPhotoCaptureSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		public  CameraCaptureUIMaxPhotoResolution MaxResolution
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "CameraCaptureUIMaxPhotoResolution CameraCaptureUIPhotoCaptureSettings.MaxResolution");
				return CameraCaptureUIMaxPhotoResolution.HighestAvailable;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "CameraCaptureUIMaxPhotoResolution CameraCaptureUIPhotoCaptureSettings.MaxResolution");
			}
		}

		#if __ANDROID__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		public CameraCaptureUIPhotoFormat Format { get; set; } = CameraCaptureUIPhotoFormat.Jpeg;

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		public  global::Windows.Foundation.Size CroppedSizeInPixels
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedSizeInPixels");
				return global::Windows.Foundation.Size.Empty;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedSizeInPixels");
			}
		}

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		public  global::Windows.Foundation.Size CroppedAspectRatio
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedAspectRatio");
				return global::Windows.Foundation.Size.Empty;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedAspectRatio");
			}
		}

		#if __ANDROID__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		public bool AllowCropping { get; set; } = true;
	}
}
