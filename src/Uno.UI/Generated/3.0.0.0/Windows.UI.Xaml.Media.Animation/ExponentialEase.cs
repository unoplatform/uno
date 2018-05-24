#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ExponentialEase : global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Exponent
		{
			get
			{
				return (double)this.GetValue(ExponentProperty);
			}
			set
			{
				this.SetValue(ExponentProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ExponentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Exponent", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Animation.ExponentialEase), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ExponentialEase() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.ExponentialEase", "ExponentialEase.ExponentialEase()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ExponentialEase.ExponentialEase()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ExponentialEase.Exponent.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ExponentialEase.Exponent.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ExponentialEase.ExponentProperty.get
	}
}
