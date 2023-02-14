#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Automation.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IItemContainerProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple FindItemByProperty( global::Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple startAfter,  global::Microsoft.UI.Xaml.Automation.AutomationProperty automationProperty,  object value);
		#endif
	}
}
