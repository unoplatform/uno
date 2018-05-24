using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using global::System.Numerics;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// RotateTransform :  Based on the WinRT Rotate transform
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.rotatetransform(v=vs.110).aspx
	/// </summary>
	public partial class RotateTransform : Transform
	{
		internal override Point Origin
		{
			get { return base.Origin; }
			set
			{
				if(Origin != value)
				{ 
					OnOriginChanged(value);
					base.Origin = value;
				}
			}
		}

		partial void OnOriginChanged(Point origin);

		public double CenterY
		{
			get { return (double)this.GetValue(CenterYProperty); }
			set { this.SetValue(CenterYProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CenterY.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CenterYProperty =
			DependencyProperty.Register("CenterY", typeof(double), typeof(RotateTransform), new PropertyMetadata(0.0, OnCenterYChanged));

		private static void OnCenterYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var self = dependencyObject as RotateTransform;

			if (self != null)
			{
				self.SetCenterY(args);
			}
		}

		partial void SetCenterY(DependencyPropertyChangedEventArgs args);

		public double CenterX
		{
			get { return (double)this.GetValue(CenterXProperty); }
			set { this.SetValue(CenterXProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CenterX.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CenterXProperty =
			DependencyProperty.Register("CenterX", typeof(double), typeof(RotateTransform), new PropertyMetadata(0.0, OnCenterXChanged));
		private static void OnCenterXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var self = dependencyObject as RotateTransform;

			if (self != null)
			{
				self.SetCenterX(args);
			}
		}

		partial void SetCenterX(DependencyPropertyChangedEventArgs args);

		public double Angle
		{
			get { return (double)this.GetValue(AngleProperty); }
			set { this.SetValue(AngleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AngleProperty =
			DependencyProperty.Register("Angle", typeof(double), typeof(RotateTransform), new PropertyMetadata(0.0, OnAngleChanged));

		private static void OnAngleChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var self = dependencyObject as RotateTransform;

			if (self != null)
			{
				self.SetAngle(args);
			}
		}

		partial void SetAngle(DependencyPropertyChangedEventArgs args);

		protected override Rect TransformBoundsCore(Rect rect) 
			=> rect.Transform(Matrix3x2.CreateRotation((float)Angle, new Vector2((float)CenterX, (float)CenterY)));
	}
}

