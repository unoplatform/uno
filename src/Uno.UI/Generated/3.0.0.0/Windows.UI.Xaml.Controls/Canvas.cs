#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Canvas : global::Windows.UI.Xaml.Controls.Panel
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LeftProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Left", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Canvas), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TopProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Top", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Canvas), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ZIndexProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"ZIndex", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Canvas), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Canvas() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Canvas", "Canvas.Canvas()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Canvas.Canvas()
		// Forced skipping of method Windows.UI.Xaml.Controls.Canvas.LeftProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static double GetLeft( global::Windows.UI.Xaml.UIElement element)
		{
			return (double)element.GetValue(LeftProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetLeft( global::Windows.UI.Xaml.UIElement element,  double length)
		{
			element.SetValue(LeftProperty, length);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Canvas.TopProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static double GetTop( global::Windows.UI.Xaml.UIElement element)
		{
			return (double)element.GetValue(TopProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetTop( global::Windows.UI.Xaml.UIElement element,  double length)
		{
			element.SetValue(TopProperty, length);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Canvas.ZIndexProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static int GetZIndex( global::Windows.UI.Xaml.UIElement element)
		{
			return (int)element.GetValue(ZIndexProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetZIndex( global::Windows.UI.Xaml.UIElement element,  int value)
		{
			element.SetValue(ZIndexProperty, value);
		}
		#endif
	}
}
