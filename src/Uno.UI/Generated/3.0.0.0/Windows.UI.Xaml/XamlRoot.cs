#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class XamlRoot 
	{
		// Skipping already declared property Content
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsHostVisible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool XamlRoot.IsHostVisible is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double RasterizationScale
		{
			get
			{
				throw new global::System.NotImplementedException("The member double XamlRoot.RasterizationScale is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Size
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.UIContext UIContext
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIContext XamlRoot.UIContext is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.XamlRoot.Content.get
		// Forced skipping of method Windows.UI.Xaml.XamlRoot.Size.get
		// Forced skipping of method Windows.UI.Xaml.XamlRoot.RasterizationScale.get
		// Forced skipping of method Windows.UI.Xaml.XamlRoot.IsHostVisible.get
		// Forced skipping of method Windows.UI.Xaml.XamlRoot.UIContext.get
		// Forced skipping of method Windows.UI.Xaml.XamlRoot.Changed.add
		// Forced skipping of method Windows.UI.Xaml.XamlRoot.Changed.remove
		// Skipping already declared event Windows.UI.Xaml.XamlRoot.Changed
	}
}
