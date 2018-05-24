using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// TranslateTransform :  Based on the WinRT TranslateTransform
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.translatetransform(v=vs.110).aspx
	/// </summary>
	public partial class TranslateTransform : Transform
	{
		public double X
		{
			get { return (double)this.GetValue(XProperty); }
			set { this.SetValue(XProperty, value); }
		}

		// Using a DependencyProperty as the backing store for X.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty XProperty =
			DependencyProperty.Register("X", typeof(double), typeof(TranslateTransform), new PropertyMetadata(0.0, OnXChanged));
		private static void OnXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyObject as TranslateTransform)?.SetX(args);
		}

		partial void SetX(DependencyPropertyChangedEventArgs args);

		public double Y
		{
			get { return (double)this.GetValue(YProperty); }
			set { this.SetValue(YProperty, value); }
		}

		// Using a DependencyProperty as the backing store for X.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty YProperty =
			DependencyProperty.Register("Y", typeof(double), typeof(TranslateTransform), new PropertyMetadata(0.0, OnYChanged));
		private static void OnYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyObject as TranslateTransform)?.SetY(args);
		}

		partial void SetY(DependencyPropertyChangedEventArgs args);

		protected override bool TryTransformCore(Point inPoint, out Point outPoint)
		{
			outPoint = inPoint + new Point(X, Y);

			return true;
		}

		protected override Rect TransformBoundsCore(Rect rect) 
			=> rect.Transform(Matrix3x2.CreateTranslation((float)X, (float)Y));
	}
}

