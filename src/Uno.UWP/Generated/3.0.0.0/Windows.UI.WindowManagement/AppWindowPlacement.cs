#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppWindowPlacement 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.DisplayRegion DisplayRegion
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayRegion AppWindowPlacement.DisplayRegion is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayRegion%20AppWindowPlacement.DisplayRegion");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point Offset
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point AppWindowPlacement.Offset is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Point%20AppWindowPlacement.Offset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Size Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size AppWindowPlacement.Size is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Size%20AppWindowPlacement.Size");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.AppWindowPlacement.DisplayRegion.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindowPlacement.Offset.get
		// Forced skipping of method Windows.UI.WindowManagement.AppWindowPlacement.Size.get
	}
}
