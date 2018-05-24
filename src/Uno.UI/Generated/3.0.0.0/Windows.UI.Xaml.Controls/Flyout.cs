#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Flyout 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style FlyoutPresenterStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(FlyoutPresenterStyleProperty);
			}
			set
			{
				this.SetValue(FlyoutPresenterStyleProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement Content
		{
			get
			{
				return (global::Windows.UI.Xaml.UIElement)this.GetValue(ContentProperty);
			}
			set
			{
				this.SetValue(ContentProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Content", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.Flyout), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FlyoutPresenterStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FlyoutPresenterStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.Flyout), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Flyout() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Flyout", "Flyout.Flyout()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Flyout.Flyout()
		// Forced skipping of method Windows.UI.Xaml.Controls.Flyout.Content.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Flyout.Content.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Flyout.FlyoutPresenterStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Flyout.FlyoutPresenterStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Flyout.ContentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Flyout.FlyoutPresenterStyleProperty.get
	}
}
