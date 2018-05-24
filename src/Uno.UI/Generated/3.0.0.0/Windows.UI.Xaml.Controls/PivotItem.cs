#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PivotItem : global::Windows.UI.Xaml.Controls.ContentControl
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.PivotItem), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PivotItem() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PivotItem", "PivotItem.PivotItem()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PivotItem.PivotItem()
		// Forced skipping of method Windows.UI.Xaml.Controls.PivotItem.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PivotItem.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PivotItem.HeaderProperty.get
	}
}
