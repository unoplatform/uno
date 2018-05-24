#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeometryGroup : global::Windows.UI.Xaml.Media.Geometry
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.FillRule FillRule
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.FillRule)this.GetValue(FillRuleProperty);
			}
			set
			{
				this.SetValue(FillRuleProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.GeometryCollection Children
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.GeometryCollection)this.GetValue(ChildrenProperty);
			}
			set
			{
				this.SetValue(ChildrenProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ChildrenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Children", typeof(global::Windows.UI.Xaml.Media.GeometryCollection), 
			typeof(global::Windows.UI.Xaml.Media.GeometryGroup), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.GeometryCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FillRuleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FillRule", typeof(global::Windows.UI.Xaml.Media.FillRule), 
			typeof(global::Windows.UI.Xaml.Media.GeometryGroup), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.FillRule)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public GeometryGroup() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.GeometryGroup", "GeometryGroup.GeometryGroup()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.GeometryGroup.GeometryGroup()
		// Forced skipping of method Windows.UI.Xaml.Media.GeometryGroup.FillRule.get
		// Forced skipping of method Windows.UI.Xaml.Media.GeometryGroup.FillRule.set
		// Forced skipping of method Windows.UI.Xaml.Media.GeometryGroup.Children.get
		// Forced skipping of method Windows.UI.Xaml.Media.GeometryGroup.Children.set
		// Forced skipping of method Windows.UI.Xaml.Media.GeometryGroup.FillRuleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.GeometryGroup.ChildrenProperty.get
	}
}
