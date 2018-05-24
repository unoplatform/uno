#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class LinearGradientBrush 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point StartPoint
		{
			get
			{
				return (global::Windows.Foundation.Point)this.GetValue(StartPointProperty);
			}
			set
			{
				this.SetValue(StartPointProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point EndPoint
		{
			get
			{
				return (global::Windows.Foundation.Point)this.GetValue(EndPointProperty);
			}
			set
			{
				this.SetValue(EndPointProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty EndPointProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"EndPoint", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Media.LinearGradientBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StartPointProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"StartPoint", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Media.LinearGradientBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public LinearGradientBrush( global::Windows.UI.Xaml.Media.GradientStopCollection gradientStopCollection,  double angle) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.LinearGradientBrush", "LinearGradientBrush.LinearGradientBrush(GradientStopCollection gradientStopCollection, double angle)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.LinearGradientBrush(Windows.UI.Xaml.Media.GradientStopCollection, double)
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public LinearGradientBrush() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.LinearGradientBrush", "LinearGradientBrush.LinearGradientBrush()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.LinearGradientBrush()
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.StartPoint.get
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.StartPoint.set
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.EndPoint.get
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.EndPoint.set
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.StartPointProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.LinearGradientBrush.EndPointProperty.get
	}
}
