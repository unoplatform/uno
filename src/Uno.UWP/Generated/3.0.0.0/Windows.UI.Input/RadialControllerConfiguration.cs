#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadialControllerConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsMenuSuppressed
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RadialControllerConfiguration.IsMenuSuppressed is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerConfiguration", "bool RadialControllerConfiguration.IsMenuSuppressed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.RadialController ActiveControllerWhenMenuIsSuppressed
		{
			get
			{
				throw new global::System.NotImplementedException("The member RadialController RadialControllerConfiguration.ActiveControllerWhenMenuIsSuppressed is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerConfiguration", "RadialController RadialControllerConfiguration.ActiveControllerWhenMenuIsSuppressed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsAppControllerEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RadialControllerConfiguration.IsAppControllerEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerConfiguration", "bool RadialControllerConfiguration.IsAppControllerEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.RadialController AppController
		{
			get
			{
				throw new global::System.NotImplementedException("The member RadialController RadialControllerConfiguration.AppController is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerConfiguration", "RadialController RadialControllerConfiguration.AppController");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDefaultMenuItems( global::System.Collections.Generic.IEnumerable<global::Windows.UI.Input.RadialControllerSystemMenuItemKind> buttons)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerConfiguration", "void RadialControllerConfiguration.SetDefaultMenuItems(IEnumerable<RadialControllerSystemMenuItemKind> buttons)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ResetToDefaultMenuItems()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialControllerConfiguration", "void RadialControllerConfiguration.ResetToDefaultMenuItems()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TrySelectDefaultMenuItem( global::Windows.UI.Input.RadialControllerSystemMenuItemKind type)
		{
			throw new global::System.NotImplementedException("The member bool RadialControllerConfiguration.TrySelectDefaultMenuItem(RadialControllerSystemMenuItemKind type) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.ActiveControllerWhenMenuIsSuppressed.set
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.ActiveControllerWhenMenuIsSuppressed.get
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.IsMenuSuppressed.set
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.IsMenuSuppressed.get
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.AppController.set
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.AppController.get
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.IsAppControllerEnabled.set
		// Forced skipping of method Windows.UI.Input.RadialControllerConfiguration.IsAppControllerEnabled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.RadialControllerConfiguration GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member RadialControllerConfiguration RadialControllerConfiguration.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
	}
}
