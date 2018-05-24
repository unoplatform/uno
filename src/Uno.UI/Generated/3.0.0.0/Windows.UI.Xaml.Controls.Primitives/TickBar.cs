#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TickBar : global::Windows.UI.Xaml.FrameworkElement
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Brush Fill
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(FillProperty);
			}
			set
			{
				this.SetValue(FillProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FillProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Fill", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.TickBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public TickBar() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.TickBar", "TickBar.TickBar()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.TickBar.TickBar()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.TickBar.Fill.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.TickBar.Fill.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.TickBar.FillProperty.get
	}
}
