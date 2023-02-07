#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Resources
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResourceLoader 
	{
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.ResourceLoader(string)
		// Forced skipping of method Windows.ApplicationModel.Resources.ResourceLoader.ResourceLoader(string)
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.ResourceLoader()
		// Forced skipping of method Windows.ApplicationModel.Resources.ResourceLoader.ResourceLoader()
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.GetString(string)
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.GetStringForUri(System.Uri)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDefaultPriPath( string packageFullName)
		{
			throw new global::System.NotImplementedException("The member string ResourceLoader.GetDefaultPriPath(string packageFullName) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ResourceLoader.GetDefaultPriPath%28string%20packageFullName%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.ResourceLoader GetForUIContext( global::Windows.UI.UIContext context)
		{
			throw new global::System.NotImplementedException("The member ResourceLoader ResourceLoader.GetForUIContext(UIContext context) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ResourceLoader%20ResourceLoader.GetForUIContext%28UIContext%20context%29");
		}
		#endif
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView()
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView(string)
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse()
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse(string)
		// Skipping already declared method Windows.ApplicationModel.Resources.ResourceLoader.GetStringForReference(System.Uri)
	}
}
