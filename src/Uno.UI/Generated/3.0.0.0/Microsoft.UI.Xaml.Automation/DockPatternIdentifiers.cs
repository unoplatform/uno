#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DockPatternIdentifiers 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.Automation.AutomationProperty DockPositionProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutomationProperty DockPatternIdentifiers.DockPositionProperty is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AutomationProperty%20DockPatternIdentifiers.DockPositionProperty");
			}
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Automation.DockPatternIdentifiers.DockPositionProperty.get
	}
}
