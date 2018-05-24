#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TranslateTransform : global::Windows.UI.Xaml.Media.Transform
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Y
		{
			get
			{
				return (double)this.GetValue(YProperty);
			}
			set
			{
				this.SetValue(YProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double X
		{
			get
			{
				return (double)this.GetValue(XProperty);
			}
			set
			{
				this.SetValue(XProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty XProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"X", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.TranslateTransform), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty YProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Y", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.TranslateTransform), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public TranslateTransform() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.TranslateTransform", "TranslateTransform.TranslateTransform()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.TranslateTransform.TranslateTransform()
		// Forced skipping of method Windows.UI.Xaml.Media.TranslateTransform.X.get
		// Forced skipping of method Windows.UI.Xaml.Media.TranslateTransform.X.set
		// Forced skipping of method Windows.UI.Xaml.Media.TranslateTransform.Y.get
		// Forced skipping of method Windows.UI.Xaml.Media.TranslateTransform.Y.set
		// Forced skipping of method Windows.UI.Xaml.Media.TranslateTransform.XProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.TranslateTransform.YProperty.get
	}
}
