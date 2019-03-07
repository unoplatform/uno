using System;
using Windows.Foundation;

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

		protected override Rect TransformBoundsCore(Rect rect)
		{
			var leftTop = Matrix.Transform(new Point(rect.Left, rect.Top));
			var rightTop = Matrix.Transform(new Point(rect.Right, rect.Top));
			var rightBottom = Matrix.Transform(new Point(rect.Right, rect.Bottom));
			var leftBottom = Matrix.Transform(new Point(rect.Left, rect.Bottom));

			var x = Math.Min(Math.Min(leftTop.X, rightTop.X), Math.Min(rightBottom.X, leftBottom.X));
			var y = Math.Min(Math.Min(leftTop.Y, rightTop.Y), Math.Min(rightBottom.Y, leftBottom.Y));

			var width = Math.Max(Math.Max(leftTop.X, rightTop.X), Math.Max(rightBottom.X, leftBottom.X)) - x;
			var height = Math.Max(Math.Max(leftTop.Y, rightTop.Y), Math.Max(rightBottom.Y, leftBottom.Y)) - y;

			return new Rect(x, y, width, height);
		}

		protected override bool TryTransformCore(Point inPoint, out Point outPoint)
		{
			outPoint = Matrix.Transform(inPoint);
			return true;
		}
	}
}
