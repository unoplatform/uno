#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreFrameworkInputViewOcclusionsChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreFrameworkInputViewOcclusionsChangedEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreFrameworkInputViewOcclusionsChangedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.ViewManagement.Core.CoreInputViewOcclusion> Occlusions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<CoreInputViewOcclusion> CoreFrameworkInputViewOcclusionsChangedEventArgs.Occlusions is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CCoreInputViewOcclusion%3E%20CoreFrameworkInputViewOcclusionsChangedEventArgs.Occlusions");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputViewOcclusionsChangedEventArgs.Occlusions.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreFrameworkInputViewOcclusionsChangedEventArgs.Handled.get
	}
}
