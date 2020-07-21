#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SelectionItemPatternIdentifiers 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Automation.AutomationProperty IsSelectedProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty SelectionItemPatternIdentifiers.IsSelectedProperty is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Automation.AutomationProperty SelectionContainerProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty SelectionItemPatternIdentifiers.SelectionContainerProperty is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.SelectionItemPatternIdentifiers.IsSelectedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Automation.SelectionItemPatternIdentifiers.SelectionContainerProperty.get
	}
}
