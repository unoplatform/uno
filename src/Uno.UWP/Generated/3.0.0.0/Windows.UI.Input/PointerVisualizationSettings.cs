#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PointerVisualizationSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsContactFeedbackEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PointerVisualizationSettings.IsContactFeedbackEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.PointerVisualizationSettings", "bool PointerVisualizationSettings.IsContactFeedbackEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsBarrelButtonFeedbackEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PointerVisualizationSettings.IsBarrelButtonFeedbackEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.PointerVisualizationSettings", "bool PointerVisualizationSettings.IsBarrelButtonFeedbackEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.PointerVisualizationSettings.IsContactFeedbackEnabled.set
		// Forced skipping of method Windows.UI.Input.PointerVisualizationSettings.IsContactFeedbackEnabled.get
		// Forced skipping of method Windows.UI.Input.PointerVisualizationSettings.IsBarrelButtonFeedbackEnabled.set
		// Forced skipping of method Windows.UI.Input.PointerVisualizationSettings.IsBarrelButtonFeedbackEnabled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.PointerVisualizationSettings GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member PointerVisualizationSettings PointerVisualizationSettings.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
	}
}
