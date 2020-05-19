#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ColorAnimationUsingKeyFrames : global::Windows.UI.Xaml.Media.Animation.Timeline
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.ColorKeyFrameCollection KeyFrames
		{
			get
			{
				throw new global::System.NotImplementedException("The member ColorKeyFrameCollection ColorAnimationUsingKeyFrames.KeyFrames is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty EnableDependentAnimationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(EnableDependentAnimation), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ColorAnimationUsingKeyFrames), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ColorAnimationUsingKeyFrames() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.ColorAnimationUsingKeyFrames", "ColorAnimationUsingKeyFrames.ColorAnimationUsingKeyFrames()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimationUsingKeyFrames.ColorAnimationUsingKeyFrames()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimationUsingKeyFrames.KeyFrames.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimationUsingKeyFrames.EnableDependentAnimation.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimationUsingKeyFrames.EnableDependentAnimation.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ColorAnimationUsingKeyFrames.EnableDependentAnimationProperty.get
	}
}
