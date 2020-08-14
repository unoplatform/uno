#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PointAnimationUsingKeyFrames : global::Windows.UI.Xaml.Media.Animation.Timeline
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool EnableDependentAnimation
		{
			get
			{
				return (bool)this.GetValue(EnableDependentAnimationProperty);
			}
			set
			{
				this.SetValue(EnableDependentAnimationProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.PointKeyFrameCollection KeyFrames
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointKeyFrameCollection PointAnimationUsingKeyFrames.KeyFrames is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty EnableDependentAnimationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(EnableDependentAnimation), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.PointAnimationUsingKeyFrames), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PointAnimationUsingKeyFrames() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.PointAnimationUsingKeyFrames", "PointAnimationUsingKeyFrames.PointAnimationUsingKeyFrames()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PointAnimationUsingKeyFrames.PointAnimationUsingKeyFrames()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PointAnimationUsingKeyFrames.KeyFrames.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PointAnimationUsingKeyFrames.EnableDependentAnimation.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PointAnimationUsingKeyFrames.EnableDependentAnimation.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PointAnimationUsingKeyFrames.EnableDependentAnimationProperty.get
	}
}
