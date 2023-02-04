#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class WindowServices 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.WindowId> FindAllTopLevelWindowIds()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<WindowId> WindowServices.FindAllTopLevelWindowIds() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CWindowId%3E%20WindowServices.FindAllTopLevelWindowIds%28%29");
		}
		#endif
	}
}
