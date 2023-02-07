#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WindowingEnvironment 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WindowingEnvironment.IsEnabled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20WindowingEnvironment.IsEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.WindowingEnvironmentKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WindowingEnvironmentKind WindowingEnvironment.Kind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WindowingEnvironmentKind%20WindowingEnvironment.Kind");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.WindowingEnvironment.IsEnabled.get
		// Forced skipping of method Windows.UI.WindowManagement.WindowingEnvironment.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.WindowManagement.DisplayRegion> GetDisplayRegions()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<DisplayRegion> WindowingEnvironment.GetDisplayRegions() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CDisplayRegion%3E%20WindowingEnvironment.GetDisplayRegions%28%29");
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.WindowingEnvironment.Changed.add
		// Forced skipping of method Windows.UI.WindowManagement.WindowingEnvironment.Changed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.WindowManagement.WindowingEnvironment> FindAll()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<WindowingEnvironment> WindowingEnvironment.FindAll() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CWindowingEnvironment%3E%20WindowingEnvironment.FindAll%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.WindowManagement.WindowingEnvironment> FindAll( global::Windows.UI.WindowManagement.WindowingEnvironmentKind kind)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<WindowingEnvironment> WindowingEnvironment.FindAll(WindowingEnvironmentKind kind) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CWindowingEnvironment%3E%20WindowingEnvironment.FindAll%28WindowingEnvironmentKind%20kind%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.WindowManagement.WindowingEnvironment, global::Windows.UI.WindowManagement.WindowingEnvironmentChangedEventArgs> Changed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.WindowingEnvironment", "event TypedEventHandler<WindowingEnvironment, WindowingEnvironmentChangedEventArgs> WindowingEnvironment.Changed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.WindowingEnvironment", "event TypedEventHandler<WindowingEnvironment, WindowingEnvironmentChangedEventArgs> WindowingEnvironment.Changed");
			}
		}
		#endif
	}
}
