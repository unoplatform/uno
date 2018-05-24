#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DoubleKeyFrame : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Value
		{
			get
			{
				return (double)this.GetValue(ValueProperty);
			}
			set
			{
				this.SetValue(ValueProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.KeyTime KeyTime
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.KeyTime)this.GetValue(KeyTimeProperty);
			}
			set
			{
				this.SetValue(KeyTimeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty KeyTimeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"KeyTime", typeof(global::Windows.UI.Xaml.Media.Animation.KeyTime), 
			typeof(global::Windows.UI.Xaml.Media.Animation.DoubleKeyFrame), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.KeyTime)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ValueProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Value", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Animation.DoubleKeyFrame), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected DoubleKeyFrame() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.DoubleKeyFrame", "DoubleKeyFrame.DoubleKeyFrame()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleKeyFrame.DoubleKeyFrame()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleKeyFrame.Value.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleKeyFrame.Value.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleKeyFrame.KeyTime.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleKeyFrame.KeyTime.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleKeyFrame.ValueProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.DoubleKeyFrame.KeyTimeProperty.get
	}
}
