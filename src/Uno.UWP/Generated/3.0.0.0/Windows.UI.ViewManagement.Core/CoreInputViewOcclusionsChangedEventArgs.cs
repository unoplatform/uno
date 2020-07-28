#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreInputViewOcclusionsChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreInputViewOcclusionsChangedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputViewOcclusionsChangedEventArgs", "bool CoreInputViewOcclusionsChangedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.ViewManagement.Core.CoreInputViewOcclusion> Occlusions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<CoreInputViewOcclusion> CoreInputViewOcclusionsChangedEventArgs.Occlusions is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewOcclusionsChangedEventArgs.Occlusions.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewOcclusionsChangedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewOcclusionsChangedEventArgs.Handled.set
	}
}
