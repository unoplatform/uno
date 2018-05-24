#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SolidColorBrush : global::Windows.UI.Xaml.Media.Brush
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color Color
		{
			get
			{
				return (global::Windows.UI.Color)this.GetValue(ColorProperty);
			}
			set
			{
				this.SetValue(ColorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Color", typeof(global::Windows.UI.Color), 
			typeof(global::Windows.UI.Xaml.Media.SolidColorBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public SolidColorBrush( global::Windows.UI.Color color) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.SolidColorBrush", "SolidColorBrush.SolidColorBrush(Color color)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.SolidColorBrush.SolidColorBrush(Windows.UI.Color)
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public SolidColorBrush() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.SolidColorBrush", "SolidColorBrush.SolidColorBrush()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.SolidColorBrush.SolidColorBrush()
		// Forced skipping of method Windows.UI.Xaml.Media.SolidColorBrush.Color.get
		// Forced skipping of method Windows.UI.Xaml.Media.SolidColorBrush.Color.set
		// Forced skipping of method Windows.UI.Xaml.Media.SolidColorBrush.ColorProperty.get
	}
}
