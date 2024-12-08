using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// ScaleTransform :  Based on the WinRT ScaleTransform
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.scaletransform%28v=vs.110%29.aspx
	/// </summary>
	public partial class ScaleTransform : Transform
	{
		internal static Matrix3x2 GetMatrix(double centerX, double centerY, double scaleX, double scaleY)
		{
			var scales = new Vector2((float)scaleX, (float)scaleY);
			var centerPoint = new Vector2((float)centerX, (float)centerY);

			return Matrix3x2.CreateScale(scales, centerPoint);
		}

		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
			=> GetMatrix(absoluteOrigin.X + CenterX, absoluteOrigin.Y + CenterY, ScaleX, ScaleY);

		public double CenterY
		{
			get => (double)this.GetValue(CenterYProperty);
			set => this.SetValue(CenterYProperty, value);
		}

		public static DependencyProperty CenterYProperty { get; } =
			DependencyProperty.Register("CenterY", typeof(double), typeof(ScaleTransform), new FrameworkPropertyMetadata(0.0, NotifyChangedCallback));

		public double CenterX
		{
			get => (double)this.GetValue(CenterXProperty);
			set => this.SetValue(CenterXProperty, value);
		}

		public static DependencyProperty CenterXProperty { get; } =
			DependencyProperty.Register("CenterX", typeof(double), typeof(ScaleTransform), new FrameworkPropertyMetadata(0.0, NotifyChangedCallback));

		public double ScaleX
		{
			get => (double)this.GetValue(ScaleXProperty);
			set => this.SetValue(ScaleXProperty, value);
		}

		public static DependencyProperty ScaleXProperty { get; } =
			DependencyProperty.Register("ScaleX", typeof(double), typeof(ScaleTransform), new FrameworkPropertyMetadata(1.0, NotifyChangedCallback));

		public double ScaleY
		{
			get => (double)this.GetValue(ScaleYProperty);
			set => this.SetValue(ScaleYProperty, value);
		}

		public static DependencyProperty ScaleYProperty { get; } =
			DependencyProperty.Register("ScaleY", typeof(double), typeof(ScaleTransform), new FrameworkPropertyMetadata(1.0, NotifyChangedCallback));
	}
}
