#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MultipleViewPatternIdentifiers 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.Automation.AutomationProperty CurrentViewProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty MultipleViewPatternIdentifiers.CurrentViewProperty is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AutomationProperty%20MultipleViewPatternIdentifiers.CurrentViewProperty");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.Automation.AutomationProperty SupportedViewsProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty MultipleViewPatternIdentifiers.SupportedViewsProperty is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AutomationProperty%20MultipleViewPatternIdentifiers.SupportedViewsProperty");
			}
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Automation.MultipleViewPatternIdentifiers.CurrentViewProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Automation.MultipleViewPatternIdentifiers.SupportedViewsProperty.get
	}
}
