using System;

namespace Windows.UI.Xaml.Media
{
	public partial class MatrixTransform : Transform
	{
		public Matrix Matrix
		{
			get => (Matrix)this.GetValue(MatrixProperty);
			set => SetValue(MatrixProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty MatrixProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Matrix", typeof(Matrix),
			typeof(MatrixTransform),
			new FrameworkPropertyMetadata(Matrix.Identity));

		public MatrixTransform()
		{

		}
	}
}
