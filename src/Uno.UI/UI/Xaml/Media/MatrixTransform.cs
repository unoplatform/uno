using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Media
{
	public partial class MatrixTransform : Transform
	{
		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
		{
			if ((absoluteOrigin.X != 0 || absoluteOrigin.Y != 0)
				&& this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("The matrix transform does not support absolute origin");
			}

			return Matrix.Inner;
		}

		public Matrix Matrix
		{
			get => (Matrix)GetValue(MatrixProperty);
			set => SetValue(MatrixProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty MatrixProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"Matrix", typeof(Matrix),
				typeof(MatrixTransform),
				new FrameworkPropertyMetadata(Matrix.Identity, NotifyChangedCallback));
	}
}
