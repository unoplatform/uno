#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.FaceAnalysis
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FaceTracker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Imaging.BitmapSize MinDetectableFaceSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member BitmapSize FaceTracker.MinDetectableFaceSize is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.FaceAnalysis.FaceTracker", "BitmapSize FaceTracker.MinDetectableFaceSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Imaging.BitmapSize MaxDetectableFaceSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member BitmapSize FaceTracker.MaxDetectableFaceSize is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.FaceAnalysis.FaceTracker", "BitmapSize FaceTracker.MaxDetectableFaceSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FaceTracker.IsSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.Media.FaceAnalysis.DetectedFace>> ProcessNextFrameAsync( global::Windows.Media.VideoFrame videoFrame)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<DetectedFace>> FaceTracker.ProcessNextFrameAsync(VideoFrame videoFrame) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.FaceAnalysis.FaceTracker.MinDetectableFaceSize.get
		// Forced skipping of method Windows.Media.FaceAnalysis.FaceTracker.MinDetectableFaceSize.set
		// Forced skipping of method Windows.Media.FaceAnalysis.FaceTracker.MaxDetectableFaceSize.get
		// Forced skipping of method Windows.Media.FaceAnalysis.FaceTracker.MaxDetectableFaceSize.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Media.FaceAnalysis.FaceTracker> CreateAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<FaceTracker> FaceTracker.CreateAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Graphics.Imaging.BitmapPixelFormat> GetSupportedBitmapPixelFormats()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<BitmapPixelFormat> FaceTracker.GetSupportedBitmapPixelFormats() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsBitmapPixelFormatSupported( global::Windows.Graphics.Imaging.BitmapPixelFormat bitmapPixelFormat)
		{
			throw new global::System.NotImplementedException("The member bool FaceTracker.IsBitmapPixelFormatSupported(BitmapPixelFormat bitmapPixelFormat) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.FaceAnalysis.FaceTracker.IsSupported.get
	}
}
