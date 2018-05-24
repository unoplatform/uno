#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Shapes
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Path 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Geometry Data
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Geometry)this.GetValue(DataProperty);
			}
			set
			{
				this.SetValue(DataProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DataProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Data", typeof(global::Windows.UI.Xaml.Media.Geometry), 
			typeof(global::Windows.UI.Xaml.Shapes.Path), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Geometry)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Path() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Shapes.Path", "Path.Path()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Shapes.Path.Path()
		// Forced skipping of method Windows.UI.Xaml.Shapes.Path.Data.get
		// Forced skipping of method Windows.UI.Xaml.Shapes.Path.Data.set
		// Forced skipping of method Windows.UI.Xaml.Shapes.Path.DataProperty.get
	}
}
