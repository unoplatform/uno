using System;
using System.Numerics;
using Windows.Foundation;

using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Media
{
	public partial class MatrixTransform : Transform
	{
		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
			=> Matrix.ToMatrix3x2().CenterOn(absoluteOrigin);

		public Matrix Matrix
		{
			get => (Matrix)GetValue(MatrixProperty);
			set => SetValue(MatrixProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty MatrixProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				"Matrix", typeof(Matrix),
				typeof(MatrixTransform),
				new FrameworkPropertyMetadata(Matrix.Identity, NotifyChangedCallback));
	}
}
