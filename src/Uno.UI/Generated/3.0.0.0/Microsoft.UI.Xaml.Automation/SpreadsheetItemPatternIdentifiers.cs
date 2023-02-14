#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpreadsheetItemPatternIdentifiers 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.Automation.AutomationProperty FormulaProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty SpreadsheetItemPatternIdentifiers.FormulaProperty is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AutomationProperty%20SpreadsheetItemPatternIdentifiers.FormulaProperty");
			}
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Automation.SpreadsheetItemPatternIdentifiers.FormulaProperty.get
	}
}
