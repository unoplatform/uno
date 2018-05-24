#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PathGeometry : global::Windows.UI.Xaml.Media.Geometry
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
		public  global::Windows.UI.Xaml.Media.PathFigureCollection Figures
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.PathFigureCollection)this.GetValue(FiguresProperty);
			}
			set
			{
				this.SetValue(FiguresProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FiguresProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Figures", typeof(global::Windows.UI.Xaml.Media.PathFigureCollection), 
			typeof(global::Windows.UI.Xaml.Media.PathGeometry), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.PathFigureCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FillRuleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FillRule", typeof(global::Windows.UI.Xaml.Media.FillRule), 
			typeof(global::Windows.UI.Xaml.Media.PathGeometry), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.FillRule)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PathGeometry() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.PathGeometry", "PathGeometry.PathGeometry()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.PathGeometry.PathGeometry()
		// Forced skipping of method Windows.UI.Xaml.Media.PathGeometry.FillRule.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathGeometry.FillRule.set
		// Forced skipping of method Windows.UI.Xaml.Media.PathGeometry.Figures.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathGeometry.Figures.set
		// Forced skipping of method Windows.UI.Xaml.Media.PathGeometry.FillRuleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathGeometry.FiguresProperty.get
	}
}
