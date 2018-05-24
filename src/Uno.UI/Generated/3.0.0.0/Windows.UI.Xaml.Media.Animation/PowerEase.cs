#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PowerEase : global::Windows.UI.Xaml.Media.Animation.EasingFunctionBase
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Power
		{
			get
			{
				return (double)this.GetValue(PowerProperty);
			}
			set
			{
				this.SetValue(PowerProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PowerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Power", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Animation.PowerEase), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PowerEase() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.PowerEase", "PowerEase.PowerEase()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PowerEase.PowerEase()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PowerEase.Power.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PowerEase.Power.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.PowerEase.PowerProperty.get
	}
}
