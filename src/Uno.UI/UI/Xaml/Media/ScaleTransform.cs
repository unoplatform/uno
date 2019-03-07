using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// ScaleTransform :  Based on the WinRT ScaleTransform
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.scaletransform%28v=vs.110%29.aspx
	/// </summary>
	public partial class ScaleTransform : Transform
	{
		public double CenterY
		{
			get => (double) GetValue(CenterYProperty);
			set => SetValue(CenterYProperty, value);
		}

		// Using a DependencyProperty as the backing store for CenterY.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CenterYProperty =
			DependencyProperty.Register("CenterY", typeof(double), typeof(ScaleTransform),
				new PropertyMetadata(0.0, OnCenterYChanged));

		private static void OnCenterYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is ScaleTransform self)
			{
				self.SetCenterY(args);
			}
		}

		partial void SetCenterY(DependencyPropertyChangedEventArgs args);

		public double CenterX
		{
			get => (double) GetValue(CenterXProperty);
			set => SetValue(CenterXProperty, value);
		}

		// Using a DependencyProperty as the backing store for CenterX.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CenterXProperty =
			DependencyProperty.Register("CenterX", typeof(double), typeof(ScaleTransform),
				new PropertyMetadata(0.0, OnCenterXChanged));

		private static void OnCenterXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is ScaleTransform self)
			{
				self.SetCenterX(args);
			}
		}

		partial void SetCenterX(DependencyPropertyChangedEventArgs args);


		public double ScaleX
		{
			get => (double) GetValue(ScaleXProperty);
			set => SetValue(ScaleXProperty, value);
		}

		// Using a DependencyProperty as the backing store for ScaleX.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ScaleXProperty =
			DependencyProperty.Register("ScaleX", typeof(double), typeof(ScaleTransform),
				new PropertyMetadata(1.0, OnScaleXChanged));

		private static void OnScaleXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is ScaleTransform self)
			{
				self.SetScaleX(args);
			}
		}

		partial void SetScaleX(DependencyPropertyChangedEventArgs args);

		public double ScaleY
		{
			get => (double) GetValue(ScaleYProperty);
			set => SetValue(ScaleYProperty, value);
		}

		// Using a DependencyProperty as the backing store for ScaleY.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ScaleYProperty =
			DependencyProperty.Register("ScaleY", typeof(double), typeof(ScaleTransform),
				new PropertyMetadata(1.0, OnScaleYChanged));

		private static void OnScaleYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is ScaleTransform self)
			{
				self.SetScaleY(args);
			}
		}

		partial void SetScaleY(DependencyPropertyChangedEventArgs args);
	}
}

