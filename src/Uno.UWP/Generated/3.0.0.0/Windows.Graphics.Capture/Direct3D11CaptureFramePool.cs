#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Direct3D11CaptureFramePool : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.DispatcherQueue DispatcherQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member DispatcherQueue Direct3D11CaptureFramePool.DispatcherQueue is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Recreate( global::Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice device,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  int numberOfBuffers,  global::Windows.Graphics.SizeInt32 size)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.Direct3D11CaptureFramePool", "void Direct3D11CaptureFramePool.Recreate(IDirect3DDevice device, DirectXPixelFormat pixelFormat, int numberOfBuffers, SizeInt32 size)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Capture.Direct3D11CaptureFrame TryGetNextFrame()
		{
			throw new global::System.NotImplementedException("The member Direct3D11CaptureFrame Direct3D11CaptureFramePool.TryGetNextFrame() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.Direct3D11CaptureFramePool.FrameArrived.add
		// Forced skipping of method Windows.Graphics.Capture.Direct3D11CaptureFramePool.FrameArrived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Capture.GraphicsCaptureSession CreateCaptureSession( global::Windows.Graphics.Capture.GraphicsCaptureItem item)
		{
			throw new global::System.NotImplementedException("The member GraphicsCaptureSession Direct3D11CaptureFramePool.CreateCaptureSession(GraphicsCaptureItem item) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.Direct3D11CaptureFramePool.DispatcherQueue.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.Direct3D11CaptureFramePool", "void Direct3D11CaptureFramePool.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Capture.Direct3D11CaptureFramePool CreateFreeThreaded( global::Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice device,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  int numberOfBuffers,  global::Windows.Graphics.SizeInt32 size)
		{
			throw new global::System.NotImplementedException("The member Direct3D11CaptureFramePool Direct3D11CaptureFramePool.CreateFreeThreaded(IDirect3DDevice device, DirectXPixelFormat pixelFormat, int numberOfBuffers, SizeInt32 size) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Capture.Direct3D11CaptureFramePool Create( global::Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice device,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  int numberOfBuffers,  global::Windows.Graphics.SizeInt32 size)
		{
			throw new global::System.NotImplementedException("The member Direct3D11CaptureFramePool Direct3D11CaptureFramePool.Create(IDirect3DDevice device, DirectXPixelFormat pixelFormat, int numberOfBuffers, SizeInt32 size) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Capture.Direct3D11CaptureFramePool, object> FrameArrived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.Direct3D11CaptureFramePool", "event TypedEventHandler<Direct3D11CaptureFramePool, object> Direct3D11CaptureFramePool.FrameArrived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.Direct3D11CaptureFramePool", "event TypedEventHandler<Direct3D11CaptureFramePool, object> Direct3D11CaptureFramePool.FrameArrived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
