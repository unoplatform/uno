#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CircleEase : global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public CircleEase() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.CircleEase", "CircleEase.CircleEase()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.CircleEase.CircleEase()
	}
}
