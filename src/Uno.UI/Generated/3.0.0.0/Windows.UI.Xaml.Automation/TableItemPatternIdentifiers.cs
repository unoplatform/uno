#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TableItemPatternIdentifiers 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Automation.AutomationProperty ColumnHeaderItemsProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty TableItemPatternIdentifiers.ColumnHeaderItemsProperty is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Automation.AutomationProperty RowHeaderItemsProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty TableItemPatternIdentifiers.RowHeaderItemsProperty is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.TableItemPatternIdentifiers.ColumnHeaderItemsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Automation.TableItemPatternIdentifiers.RowHeaderItemsProperty.get
	}
}
