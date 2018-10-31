#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserControl 
	{
		#if false || false || false || false || false
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
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Content", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.UserControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public UserControl() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.UserControl", "UserControl.UserControl()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.UserControl.UserControl()
		// Forced skipping of method Windows.UI.Xaml.Controls.UserControl.Content.get
		// Forced skipping of method Windows.UI.Xaml.Controls.UserControl.Content.set
		// Forced skipping of method Windows.UI.Xaml.Controls.UserControl.ContentProperty.get
	}
}
