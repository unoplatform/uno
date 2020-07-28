#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CommandBarFlyoutCommandBar : global::Windows.UI.Xaml.Controls.CommandBar
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBarTemplateSettings FlyoutTemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member CommandBarFlyoutCommandBarTemplateSettings CommandBarFlyoutCommandBar.FlyoutTemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CommandBarFlyoutCommandBar() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar", "CommandBarFlyoutCommandBar.CommandBarFlyoutCommandBar()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar.CommandBarFlyoutCommandBar()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar.FlyoutTemplateSettings.get
	}
}
