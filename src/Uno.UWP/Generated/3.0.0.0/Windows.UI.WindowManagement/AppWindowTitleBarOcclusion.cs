#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppWindowTitleBarOcclusion 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect OccludingRect
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect AppWindowTitleBarOcclusion.OccludingRect is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Rect%20AppWindowTitleBarOcclusion.OccludingRect");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.AppWindowTitleBarOcclusion.OccludingRect.get
	}
}
