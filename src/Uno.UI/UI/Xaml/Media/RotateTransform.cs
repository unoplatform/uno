using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using global::System.Numerics;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// RotateTransform :  Based on the WinRT Rotate transform
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.rotatetransform(v=vs.110).aspx
	/// </summary>
	public sealed partial class RotateTransform : Transform
	{
		internal static Matrix3x2 GetMatrix(double centerX, double centerY, double angleDegree)
		{
			var angle = (float)MathEx.ToRadians(angleDegree);
			var centerPoint = new Vector2((float)centerX, (float)centerY);

			return Matrix3x2.CreateRotation(angle, centerPoint);
		}

		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
			=> GetMatrix(absoluteOrigin.X + CenterX, absoluteOrigin.Y + CenterY, Angle);


		public double CenterY
		{
			get => (double)this.GetValue(CenterYProperty);
			set => this.SetValue(CenterYProperty, value);
		}

		public static DependencyProperty CenterYProperty { get; } =
			DependencyProperty.Register("CenterY", typeof(double), typeof(RotateTransform), new FrameworkPropertyMetadata(0.0, NotifyChangedCallback));

		public double CenterX
		{
			get => (double)this.GetValue(CenterXProperty);
			set => this.SetValue(CenterXProperty, value);
		}

		public static DependencyProperty CenterXProperty { get; } =
			DependencyProperty.Register("CenterX", typeof(double), typeof(RotateTransform), new FrameworkPropertyMetadata(0.0, NotifyChangedCallback));

		public double Angle
		{
			get => (double)this.GetValue(AngleProperty);
			set => this.SetValue(AngleProperty, value);
		}

		public static DependencyProperty AngleProperty { get; } =
			DependencyProperty.Register("Angle", typeof(double), typeof(RotateTransform), new FrameworkPropertyMetadata(0.0, NotifyChangedCallback));
	}
}

