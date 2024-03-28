using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// CompositeTransform :  Based on the WinRT Composite transform
	/// https://searchcode.com/codesearch/view/10522146/
	/// </summary>
	public partial class CompositeTransform : Transform
	{
		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
		{
			// Creates native transform which applies multiple transformations in this order:
			// Scale(ScaleX, ScaleY)
			// Skew(SkewX, SkewY)
			// Rotate(Rotation)
			// Translate(TranslateX, TranslateY)
			// https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.media.compositetransform.aspx

			var centerX = absoluteOrigin.X + CenterX;
			var centerY = absoluteOrigin.Y + CenterY;

			var matrix = Matrix3x2.Identity;

			matrix *= ScaleTransform.GetMatrix(centerX, centerY, ScaleX, ScaleY);
			matrix *= SkewTransform.GetMatrix(CenterX, CenterY, SkewX, SkewY);
			matrix *= RotateTransform.GetMatrix(CenterX, CenterY, Rotation);
			matrix *= TranslateTransform.GetMatrix(TranslateX, TranslateY);

			return matrix;
		}

		public double CenterX
		{
			get => (double)this.GetValue(CenterXProperty);
			set => this.SetValue(CenterXProperty, value);
		}

		public static DependencyProperty CenterXProperty { get; } =
			DependencyProperty.Register("CenterX", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(0.0d, NotifyChangedCallback));

		public double CenterY
		{
			get => (double)this.GetValue(CenterYProperty);
			set => this.SetValue(CenterYProperty, value);
		}

		public static DependencyProperty CenterYProperty { get; } =
			DependencyProperty.Register("CenterY", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(0.0d, NotifyChangedCallback));

		public double Rotation
		{
			get => (double)this.GetValue(RotationProperty);
			set => this.SetValue(RotationProperty, value);
		}

		public static DependencyProperty RotationProperty { get; } =
			DependencyProperty.Register("Rotation", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(0.0d, NotifyChangedCallback));

		public double ScaleX
		{
			get => (double)this.GetValue(ScaleXProperty);
			set => this.SetValue(ScaleXProperty, value);
		}

		public static DependencyProperty ScaleXProperty { get; } =
			DependencyProperty.Register("ScaleX", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(1.0d, NotifyChangedCallback));

		public double ScaleY
		{
			get => (double)this.GetValue(ScaleYProperty);
			set => this.SetValue(ScaleYProperty, value);
		}

		public static DependencyProperty ScaleYProperty { get; } =
			DependencyProperty.Register("ScaleY", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(1.0d, NotifyChangedCallback));

		public double SkewX
		{
			get => (double)this.GetValue(SkewXProperty);
			set => this.SetValue(SkewXProperty, value);
		}

		public static DependencyProperty SkewXProperty { get; } =
			DependencyProperty.Register("SkewX", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(0.0d, NotifyChangedCallback));

		public double SkewY
		{
			get => (double)this.GetValue(SkewYProperty);
			set => this.SetValue(SkewYProperty, value);
		}

		public static DependencyProperty SkewYProperty { get; } =
			DependencyProperty.Register("SkewY", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(0.0d, NotifyChangedCallback));

		public double TranslateX
		{
			get => (double)this.GetValue(TranslateXProperty);
			set => this.SetValue(TranslateXProperty, value);
		}

		public static DependencyProperty TranslateXProperty { get; } =
			DependencyProperty.Register("TranslateX", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(0.0d, NotifyChangedCallback));

		public double TranslateY
		{
			get => (double)this.GetValue(TranslateYProperty);
			set => this.SetValue(TranslateYProperty, value);
		}

		public static DependencyProperty TranslateYProperty { get; } =
			DependencyProperty.Register("TranslateY", typeof(double), typeof(CompositeTransform), new FrameworkPropertyMetadata(0.0d, NotifyChangedCallback));

	}
}
