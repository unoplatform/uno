#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CameraCaptureUI 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings PhotoSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member CameraCaptureUIPhotoCaptureSettings CameraCaptureUI.PhotoSettings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings VideoSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member CameraCaptureUIVideoCaptureSettings CameraCaptureUI.VideoSettings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public CameraCaptureUI() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUI", "CameraCaptureUI.CameraCaptureUI()");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUI.CameraCaptureUI()
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUI.PhotoSettings.get
		// Forced skipping of method Windows.Media.Capture.CameraCaptureUI.VideoSettings.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CaptureFileAsync( global::Windows.Media.Capture.CameraCaptureUIMode mode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> CameraCaptureUI.CaptureFileAsync(CameraCaptureUIMode mode) is not implemented in Uno.");
		}
		#endif
	}
}
