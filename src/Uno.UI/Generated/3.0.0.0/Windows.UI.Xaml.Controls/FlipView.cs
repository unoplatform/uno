#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FlipView 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool UseTouchAnimationsForAllNavigation
		{
			get
			{
				return (bool)this.GetValue(UseTouchAnimationsForAllNavigationProperty);
			}
			set
			{
				this.SetValue(UseTouchAnimationsForAllNavigationProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty UseTouchAnimationsForAllNavigationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"UseTouchAnimationsForAllNavigation", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.FlipView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public FlipView() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.FlipView", "FlipView.FlipView()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.FlipView.FlipView()
		// Forced skipping of method Windows.UI.Xaml.Controls.FlipView.UseTouchAnimationsForAllNavigation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.FlipView.UseTouchAnimationsForAllNavigation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.FlipView.UseTouchAnimationsForAllNavigationProperty.get
	}
}
