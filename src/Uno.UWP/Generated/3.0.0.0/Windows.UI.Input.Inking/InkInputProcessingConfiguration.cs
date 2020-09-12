#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkInputProcessingConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkInputRightDragAction RightDragAction
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkInputRightDragAction InkInputProcessingConfiguration.RightDragAction is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkInputProcessingConfiguration", "InkInputRightDragAction InkInputProcessingConfiguration.RightDragAction");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkInputProcessingMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkInputProcessingMode InkInputProcessingConfiguration.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.Inking.InkInputProcessingConfiguration", "InkInputProcessingMode InkInputProcessingConfiguration.Mode");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkInputProcessingConfiguration.Mode.get
		// Forced skipping of method Windows.UI.Input.Inking.InkInputProcessingConfiguration.Mode.set
		// Forced skipping of method Windows.UI.Input.Inking.InkInputProcessingConfiguration.RightDragAction.get
		// Forced skipping of method Windows.UI.Input.Inking.InkInputProcessingConfiguration.RightDragAction.set
	}
}
