#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DoubleAnimationUsingKeyFrames : global::Windows.UI.Xaml.Media.Animation.Timeline
	{
		#if false || false || false || false
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.DoubleKeyFrameCollection KeyFrames
		{
			get
			{
				throw new global::System.NotImplementedException("The member DoubleKeyFrameCollection DoubleAnimationUsingKeyFrames.KeyFrames is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty EnableDependentAnimationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"EnableDependentAnimation", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public DoubleAnimationUsingKeyFrames() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames", "DoubleAnimationUsingKeyFrames.DoubleAnimationUsingKeyFrames()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames.DoubleAnimationUsingKeyFrames()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames.KeyFrames.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames.EnableDependentAnimation.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames.EnableDependentAnimation.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames.EnableDependentAnimationProperty.get
	}
}
