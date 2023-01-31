#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GraphicsCaptureSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCursorCaptureEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GraphicsCaptureSession.IsCursorCaptureEnabled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20GraphicsCaptureSession.IsCursorCaptureEnabled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureSession", "bool GraphicsCaptureSession.IsCursorCaptureEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsBorderRequired
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GraphicsCaptureSession.IsBorderRequired is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20GraphicsCaptureSession.IsBorderRequired");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureSession", "bool GraphicsCaptureSession.IsBorderRequired");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartCapture()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureSession", "void GraphicsCaptureSession.StartCapture()");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureSession.IsCursorCaptureEnabled.get
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureSession.IsCursorCaptureEnabled.set
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureSession.IsBorderRequired.get
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureSession.IsBorderRequired.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureSession", "void GraphicsCaptureSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool GraphicsCaptureSession.IsSupported() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20GraphicsCaptureSession.IsSupported%28%29");
		}
		#endif
		// Processing: System.IDisposable
	}
}
