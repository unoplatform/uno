#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BeginStoryboard : global::Microsoft.UI.Xaml.TriggerAction
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Media.Animation.Storyboard Storyboard
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Media.Animation.Storyboard)this.GetValue(StoryboardProperty);
			}
			set
			{
				this.SetValue(StoryboardProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty StoryboardProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Storyboard), typeof(global::Microsoft.UI.Xaml.Media.Animation.Storyboard), 
			typeof(global::Microsoft.UI.Xaml.Media.Animation.BeginStoryboard), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Media.Animation.Storyboard)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BeginStoryboard() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Media.Animation.BeginStoryboard", "BeginStoryboard.BeginStoryboard()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.BeginStoryboard.BeginStoryboard()
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.BeginStoryboard.Storyboard.get
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.BeginStoryboard.Storyboard.set
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.BeginStoryboard.StoryboardProperty.get
	}
}
