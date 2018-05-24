#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class HyperlinkButton : global::Windows.UI.Xaml.Controls.Primitives.ButtonBase
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Uri NavigateUri
		{
			get
			{
				return (global::System.Uri)this.GetValue(NavigateUriProperty);
			}
			set
			{
				this.SetValue(NavigateUriProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty NavigateUriProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"NavigateUri", typeof(global::System.Uri), 
			typeof(global::Windows.UI.Xaml.Controls.HyperlinkButton), 
			new FrameworkPropertyMetadata(default(global::System.Uri)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public HyperlinkButton() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.HyperlinkButton", "HyperlinkButton.HyperlinkButton()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.HyperlinkButton.HyperlinkButton()
		// Forced skipping of method Windows.UI.Xaml.Controls.HyperlinkButton.NavigateUri.get
		// Forced skipping of method Windows.UI.Xaml.Controls.HyperlinkButton.NavigateUri.set
		// Forced skipping of method Windows.UI.Xaml.Controls.HyperlinkButton.NavigateUriProperty.get
	}
}
