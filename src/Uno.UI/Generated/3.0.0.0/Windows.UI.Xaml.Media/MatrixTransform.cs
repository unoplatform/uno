#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MatrixTransform : global::Windows.UI.Xaml.Media.Transform
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Matrix Matrix
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Matrix)this.GetValue(MatrixProperty);
			}
			set
			{
				this.SetValue(MatrixProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MatrixProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Matrix", typeof(global::Windows.UI.Xaml.Media.Matrix), 
			typeof(global::Windows.UI.Xaml.Media.MatrixTransform), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Matrix)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public MatrixTransform() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.MatrixTransform", "MatrixTransform.MatrixTransform()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.MatrixTransform.MatrixTransform()
		// Forced skipping of method Windows.UI.Xaml.Media.MatrixTransform.Matrix.get
		// Forced skipping of method Windows.UI.Xaml.Media.MatrixTransform.Matrix.set
		// Forced skipping of method Windows.UI.Xaml.Media.MatrixTransform.MatrixProperty.get
	}
}
