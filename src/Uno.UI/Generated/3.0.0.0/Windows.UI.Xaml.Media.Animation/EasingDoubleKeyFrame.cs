#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class EasingDoubleKeyFrame : global::Windows.UI.Xaml.Media.Animation.DoubleKeyFrame
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase EasingFunction
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase)this.GetValue(EasingFunctionProperty);
			}
			set
			{
				this.SetValue(EasingFunctionProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty EasingFunctionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"EasingFunction", typeof(global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase), 
			typeof(global::Windows.UI.Xaml.Media.Animation.EasingDoubleKeyFrame), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public EasingDoubleKeyFrame() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.EasingDoubleKeyFrame", "EasingDoubleKeyFrame.EasingDoubleKeyFrame()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EasingDoubleKeyFrame.EasingDoubleKeyFrame()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EasingDoubleKeyFrame.EasingFunction.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EasingDoubleKeyFrame.EasingFunction.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EasingDoubleKeyFrame.EasingFunctionProperty.get
	}
}
