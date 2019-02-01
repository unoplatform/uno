using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// ScaleTransform :  Based on the WinRT ScaleTransform
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.skewtransform(v=vs.110).aspx
	/// </summary>
	public partial class SkewTransform : Transform
	{
		internal static Matrix3x2 GetMatrix(double centerX, double centerY, double angleXDegree, double angleYDegree)
		{
			var angleX = (float) MathEx.ToRadians(angleXDegree);
			var angleY = (float) MathEx.ToRadians(angleYDegree);
			var centerPoint = new Vector2((float)centerX, (float)centerY);

			return Matrix3x2.CreateSkew(angleX, angleY, centerPoint);
		}

		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
			=> GetMatrix(absoluteOrigin.X + CenterX, absoluteOrigin.Y + CenterY, AngleX, AngleY);

		public double CenterY
		{
			get => (double)this.GetValue(CenterYProperty);
			set => this.SetValue(CenterYProperty, value);
		}

		public static readonly DependencyProperty CenterYProperty =
			DependencyProperty.Register("CenterY", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, NotifyChangedCallback));

		public double CenterX
		{
			get => (double)this.GetValue(CenterXProperty);
			set => this.SetValue(CenterXProperty, value);
		}

		public static readonly DependencyProperty CenterXProperty =
			DependencyProperty.Register("CenterX", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, NotifyChangedCallback));

		public double AngleX
		{
			get => (double)this.GetValue(AngleXProperty);
			set => this.SetValue(AngleXProperty, value);
		}

		public static readonly DependencyProperty AngleXProperty =
			DependencyProperty.Register("AngleX", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, NotifyChangedCallback));


		public double AngleY
		{
			get => (double)this.GetValue(AngleYProperty);
			set => this.SetValue(AngleYProperty, value);
		}

		public static readonly DependencyProperty AngleYProperty =
			DependencyProperty.Register("AngleY", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, NotifyChangedCallback));
	}
}
