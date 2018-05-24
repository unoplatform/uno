#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackEase : global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Amplitude
		{
			get
			{
				return (double)this.GetValue(AmplitudeProperty);
			}
			set
			{
				this.SetValue(AmplitudeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AmplitudeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Amplitude", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Animation.BackEase), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public BackEase() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.BackEase", "BackEase.BackEase()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.BackEase.BackEase()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.BackEase.Amplitude.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.BackEase.Amplitude.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.BackEase.AmplitudeProperty.get
	}
}
