#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GravityConnectedAnimationConfiguration : global::Windows.UI.Xaml.Media.Animation.ConnectedAnimationConfiguration
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsShadowEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GravityConnectedAnimationConfiguration.IsShadowEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.GravityConnectedAnimationConfiguration", "bool GravityConnectedAnimationConfiguration.IsShadowEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GravityConnectedAnimationConfiguration() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.GravityConnectedAnimationConfiguration", "GravityConnectedAnimationConfiguration.GravityConnectedAnimationConfiguration()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.GravityConnectedAnimationConfiguration.GravityConnectedAnimationConfiguration()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.GravityConnectedAnimationConfiguration.IsShadowEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.GravityConnectedAnimationConfiguration.IsShadowEnabled.set
	}
}
