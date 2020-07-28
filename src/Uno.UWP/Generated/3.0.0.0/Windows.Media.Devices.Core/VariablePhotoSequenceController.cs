#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VariablePhotoSequenceController 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float PhotosPerSecondLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member float VariablePhotoSequenceController.PhotosPerSecondLimit is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.VariablePhotoSequenceController", "float VariablePhotoSequenceController.PhotosPerSecondLimit");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Devices.Core.FrameController> DesiredFrameControllers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<FrameController> VariablePhotoSequenceController.DesiredFrameControllers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.Core.FrameControlCapabilities FrameCapabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member FrameControlCapabilities VariablePhotoSequenceController.FrameCapabilities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float MaxPhotosPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member float VariablePhotoSequenceController.MaxPhotosPerSecond is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VariablePhotoSequenceController.Supported is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.Core.VariablePhotoSequenceController.Supported.get
		// Forced skipping of method Windows.Media.Devices.Core.VariablePhotoSequenceController.MaxPhotosPerSecond.get
		// Forced skipping of method Windows.Media.Devices.Core.VariablePhotoSequenceController.PhotosPerSecondLimit.get
		// Forced skipping of method Windows.Media.Devices.Core.VariablePhotoSequenceController.PhotosPerSecondLimit.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaRatio GetHighestConcurrentFrameRate( global::Windows.Media.MediaProperties.IMediaEncodingProperties captureProperties)
		{
			throw new global::System.NotImplementedException("The member MediaRatio VariablePhotoSequenceController.GetHighestConcurrentFrameRate(IMediaEncodingProperties captureProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaRatio GetCurrentFrameRate()
		{
			throw new global::System.NotImplementedException("The member MediaRatio VariablePhotoSequenceController.GetCurrentFrameRate() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.Core.VariablePhotoSequenceController.FrameCapabilities.get
		// Forced skipping of method Windows.Media.Devices.Core.VariablePhotoSequenceController.DesiredFrameControllers.get
	}
}
