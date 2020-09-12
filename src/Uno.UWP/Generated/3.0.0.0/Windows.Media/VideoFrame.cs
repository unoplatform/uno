#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VideoFrame : global::Windows.Media.IMediaFrame,global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? SystemRelativeTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? VideoFrame.SystemRelativeTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoFrame", "TimeSpan? VideoFrame.SystemRelativeTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? RelativeTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? VideoFrame.RelativeTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoFrame", "TimeSpan? VideoFrame.RelativeTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDiscontinuous
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VideoFrame.IsDiscontinuous is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoFrame", "bool VideoFrame.IsDiscontinuous");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? VideoFrame.Duration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoFrame", "TimeSpan? VideoFrame.Duration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IPropertySet ExtendedProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet VideoFrame.ExtendedProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsReadOnly
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VideoFrame.IsReadOnly is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VideoFrame.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface Direct3DSurface
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDirect3DSurface VideoFrame.Direct3DSurface is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Imaging.SoftwareBitmap SoftwareBitmap
		{
			get
			{
				throw new global::System.NotImplementedException("The member SoftwareBitmap VideoFrame.SoftwareBitmap is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VideoFrame( global::Windows.Graphics.Imaging.BitmapPixelFormat format,  int width,  int height) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoFrame", "VideoFrame.VideoFrame(BitmapPixelFormat format, int width, int height)");
		}
		#endif
		// Forced skipping of method Windows.Media.VideoFrame.VideoFrame(Windows.Graphics.Imaging.BitmapPixelFormat, int, int)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VideoFrame( global::Windows.Graphics.Imaging.BitmapPixelFormat format,  int width,  int height,  global::Windows.Graphics.Imaging.BitmapAlphaMode alpha) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoFrame", "VideoFrame.VideoFrame(BitmapPixelFormat format, int width, int height, BitmapAlphaMode alpha)");
		}
		#endif
		// Forced skipping of method Windows.Media.VideoFrame.VideoFrame(Windows.Graphics.Imaging.BitmapPixelFormat, int, int, Windows.Graphics.Imaging.BitmapAlphaMode)
		// Forced skipping of method Windows.Media.VideoFrame.SoftwareBitmap.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CopyToAsync( global::Windows.Media.VideoFrame frame)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VideoFrame.CopyToAsync(VideoFrame frame) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.VideoFrame.Direct3DSurface.get
		// Forced skipping of method Windows.Media.VideoFrame.Type.get
		// Forced skipping of method Windows.Media.VideoFrame.IsReadOnly.get
		// Forced skipping of method Windows.Media.VideoFrame.RelativeTime.set
		// Forced skipping of method Windows.Media.VideoFrame.RelativeTime.get
		// Forced skipping of method Windows.Media.VideoFrame.SystemRelativeTime.set
		// Forced skipping of method Windows.Media.VideoFrame.SystemRelativeTime.get
		// Forced skipping of method Windows.Media.VideoFrame.Duration.set
		// Forced skipping of method Windows.Media.VideoFrame.Duration.get
		// Forced skipping of method Windows.Media.VideoFrame.IsDiscontinuous.set
		// Forced skipping of method Windows.Media.VideoFrame.IsDiscontinuous.get
		// Forced skipping of method Windows.Media.VideoFrame.ExtendedProperties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoFrame", "void VideoFrame.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CopyToAsync( global::Windows.Media.VideoFrame frame,  global::Windows.Graphics.Imaging.BitmapBounds? sourceBounds,  global::Windows.Graphics.Imaging.BitmapBounds? destinationBounds)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VideoFrame.CopyToAsync(VideoFrame frame, BitmapBounds? sourceBounds, BitmapBounds? destinationBounds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.VideoFrame CreateAsDirect3D11SurfaceBacked( global::Windows.Graphics.DirectX.DirectXPixelFormat format,  int width,  int height)
		{
			throw new global::System.NotImplementedException("The member VideoFrame VideoFrame.CreateAsDirect3D11SurfaceBacked(DirectXPixelFormat format, int width, int height) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.VideoFrame CreateAsDirect3D11SurfaceBacked( global::Windows.Graphics.DirectX.DirectXPixelFormat format,  int width,  int height,  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice device)
		{
			throw new global::System.NotImplementedException("The member VideoFrame VideoFrame.CreateAsDirect3D11SurfaceBacked(DirectXPixelFormat format, int width, int height, IDirect3DDevice device) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.VideoFrame CreateWithSoftwareBitmap( global::Windows.Graphics.Imaging.SoftwareBitmap bitmap)
		{
			throw new global::System.NotImplementedException("The member VideoFrame VideoFrame.CreateWithSoftwareBitmap(SoftwareBitmap bitmap) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.VideoFrame CreateWithDirect3D11Surface( global::Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface surface)
		{
			throw new global::System.NotImplementedException("The member VideoFrame VideoFrame.CreateWithDirect3D11Surface(IDirect3DSurface surface) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Media.IMediaFrame
		// Processing: System.IDisposable
	}
}
