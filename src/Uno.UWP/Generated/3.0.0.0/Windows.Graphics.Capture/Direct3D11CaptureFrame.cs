#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Direct3D11CaptureFrame : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.SizeInt32 ContentSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member SizeInt32 Direct3D11CaptureFrame.ContentSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface Surface
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDirect3DSurface Direct3D11CaptureFrame.Surface is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan SystemRelativeTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan Direct3D11CaptureFrame.SystemRelativeTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.Direct3D11CaptureFrame.Surface.get
		// Forced skipping of method Windows.Graphics.Capture.Direct3D11CaptureFrame.SystemRelativeTime.get
		// Forced skipping of method Windows.Graphics.Capture.Direct3D11CaptureFrame.ContentSize.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.Direct3D11CaptureFrame", "void Direct3D11CaptureFrame.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
