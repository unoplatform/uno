#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class GraphicsCaptureAccess 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus> RequestAccessAsync( global::Windows.Graphics.Capture.GraphicsCaptureAccessKind request)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppCapabilityAccessStatus> GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind request) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CAppCapabilityAccessStatus%3E%20GraphicsCaptureAccess.RequestAccessAsync%28GraphicsCaptureAccessKind%20request%29");
		}
		#endif
	}
}
