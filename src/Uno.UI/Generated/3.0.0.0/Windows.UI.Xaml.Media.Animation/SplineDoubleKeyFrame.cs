#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SplineDoubleKeyFrame : global::Windows.UI.Xaml.Media.Animation.DoubleKeyFrame
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.KeySpline KeySpline
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.KeySpline)this.GetValue(KeySplineProperty);
			}
			set
			{
				this.SetValue(KeySplineProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty KeySplineProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"KeySpline", typeof(global::Windows.UI.Xaml.Media.Animation.KeySpline), 
			typeof(global::Windows.UI.Xaml.Media.Animation.SplineDoubleKeyFrame), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.KeySpline)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public SplineDoubleKeyFrame() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.SplineDoubleKeyFrame", "SplineDoubleKeyFrame.SplineDoubleKeyFrame()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.SplineDoubleKeyFrame.SplineDoubleKeyFrame()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.SplineDoubleKeyFrame.KeySpline.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.SplineDoubleKeyFrame.KeySpline.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.SplineDoubleKeyFrame.KeySplineProperty.get
	}
}
