#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionVideoFrameAllocator : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PerceptionVideoFrameAllocator( uint maxOutstandingFrameCountForWrite,  global::Windows.Graphics.Imaging.BitmapPixelFormat format,  global::Windows.Foundation.Size resolution,  global::Windows.Graphics.Imaging.BitmapAlphaMode alpha) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.Provider.PerceptionVideoFrameAllocator", "PerceptionVideoFrameAllocator.PerceptionVideoFrameAllocator(uint maxOutstandingFrameCountForWrite, BitmapPixelFormat format, Size resolution, BitmapAlphaMode alpha)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionVideoFrameAllocator.PerceptionVideoFrameAllocator(uint, Windows.Graphics.Imaging.BitmapPixelFormat, Windows.Foundation.Size, Windows.Graphics.Imaging.BitmapAlphaMode)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.Provider.PerceptionFrame AllocateFrame()
		{
			throw new global::System.NotImplementedException("The member PerceptionFrame PerceptionVideoFrameAllocator.AllocateFrame() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.Provider.PerceptionFrame CopyFromVideoFrame( global::Windows.Media.VideoFrame frame)
		{
			throw new global::System.NotImplementedException("The member PerceptionFrame PerceptionVideoFrameAllocator.CopyFromVideoFrame(VideoFrame frame) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.Provider.PerceptionVideoFrameAllocator", "void PerceptionVideoFrameAllocator.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
