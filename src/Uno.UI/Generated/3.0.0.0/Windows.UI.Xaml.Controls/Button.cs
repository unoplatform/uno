#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Button : global::Windows.UI.Xaml.Controls.Primitives.ButtonBase
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase Flyout
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)this.GetValue(FlyoutProperty);
			}
			set
			{
				this.SetValue(FlyoutProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FlyoutProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Flyout", typeof(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase), 
			typeof(global::Windows.UI.Xaml.Controls.Button), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Button() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Button", "Button.Button()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Button.Button()
		// Forced skipping of method Windows.UI.Xaml.Controls.Button.Flyout.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Button.Flyout.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Button.FlyoutProperty.get
	}
}
