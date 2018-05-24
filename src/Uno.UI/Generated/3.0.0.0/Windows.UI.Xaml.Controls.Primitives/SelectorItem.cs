#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SelectorItem : global::Windows.UI.Xaml.Controls.ContentControl
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsSelected
		{
			get
			{
				return (bool)this.GetValue(IsSelectedProperty);
			}
			set
			{
				this.SetValue(IsSelectedProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsSelectedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSelected", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.SelectorItem), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected SelectorItem() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.SelectorItem", "SelectorItem.SelectorItem()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.SelectorItem.SelectorItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.SelectorItem.IsSelected.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.SelectorItem.IsSelected.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.SelectorItem.IsSelectedProperty.get
	}
}
