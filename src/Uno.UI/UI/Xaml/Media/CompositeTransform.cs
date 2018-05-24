using System;
using System.Collections.Generic;
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
		private readonly ScaleTransform _scale = new ScaleTransform();
		private readonly SkewTransform _skew = new SkewTransform();
		private readonly RotateTransform _rotation = new RotateTransform();
		private readonly TranslateTransform _translation = new TranslateTransform();
		//TODO: this doubles up on the 'ToNativeTransform' method and should be removed.
		private readonly Transform _innerTransform;


		public CompositeTransform()
		{
			// Creates native transform which applies multiple transformations in this order:
			// Scale(ScaleX, ScaleY )
			// Skew(SkewX, SkewY)
			// Rotate(Rotation)
			// Translate(TranslateX, TranslateY)
			// https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.media.compositetransform.aspx

			_innerTransform = new TransformGroup
			{
				Children = new TransformCollection
				{
					_scale,
					_skew,
					_rotation,
					_translation
				}
			};
		}

		internal override void OnViewSizeChanged(Size oldSize, Size newSize)
		{
			_innerTransform.OnViewSizeChanged(oldSize, newSize);
		}

		internal override Point Origin
        {
            get => _innerTransform.Origin;
			set => _innerTransform.Origin = value;
		}

        public double CenterX
		{
			get => (double)this.GetValue(CenterXProperty);
			set => this.SetValue(CenterXProperty, value);
		}

		public static readonly DependencyProperty CenterXProperty =
			DependencyProperty.Register("CenterX", typeof(double), typeof(CompositeTransform), new PropertyMetadata(0.0d, OnCenterXChanged));

		private static void OnCenterXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			// Update the internal value if the value is being animated.
			// The value is not being animated by the platform itself.

			if (dependencyObject is CompositeTransform transform)
			{
				transform._scale.CenterX = transform.CenterX;
				transform._skew.CenterX = transform.CenterX;
				transform._rotation.CenterX = transform.CenterX;
			}
		}

		public double CenterY
		{
			get => (double)this.GetValue(CenterYProperty);
			set => this.SetValue(CenterYProperty, value);
		}

		public static readonly DependencyProperty CenterYProperty =
			DependencyProperty.Register("CenterY", typeof(double), typeof(CompositeTransform), new PropertyMetadata(0.0d, OnCenterYChanged));
		private static void OnCenterYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            // Update the internal value if the value is being animated.
            // The value is not being animated by the platform itself.

			if (dependencyObject is CompositeTransform transform)
			{
				transform._scale.CenterY = transform.CenterY;
				transform._skew.CenterY = transform.CenterY;
				transform._rotation.CenterY = transform.CenterY;
			}
		}

		public double Rotation
		{
			get => (double)this.GetValue(RotationProperty);
			set => this.SetValue(RotationProperty, value);
		}

		public static readonly DependencyProperty RotationProperty =
			DependencyProperty.Register("Rotation", typeof(double), typeof(CompositeTransform), new PropertyMetadata(0.0d, OnRotationChanged));
		private static void OnRotationChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
            // Don't update the internal value if the value is being animated.
            // The value is being animated by the platform itself.
            if (args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation)
            {
                return;
            }

			if (dependencyObject is CompositeTransform transform)
			{
				transform._rotation.Angle = transform.Rotation;
			}
		}

		public double ScaleX
		{
			get => (double)this.GetValue(ScaleXProperty);
			set => this.SetValue(ScaleXProperty, value);
		}

		public static readonly DependencyProperty ScaleXProperty =
			DependencyProperty.Register("ScaleX", typeof(double), typeof(CompositeTransform), new PropertyMetadata(1.0d, OnScaleXChanged));
		private static void OnScaleXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
            // Don't update the internal value if the value is being animated.
            // The value is being animated by the platform itself.
            if (args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation)
            {
                return;
            }

			if (dependencyObject is CompositeTransform transform)
			{
				transform._scale.ScaleX = transform.ScaleX;
			}
		}

		public double ScaleY
		{
			get => (double)this.GetValue(ScaleYProperty);
			set => this.SetValue(ScaleYProperty, value);
		}

		public static readonly DependencyProperty ScaleYProperty =
			DependencyProperty.Register("ScaleY", typeof(double), typeof(CompositeTransform), new PropertyMetadata(1.0d, OnScaleYChanged));
		private static void OnScaleYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
            // Don't update the internal value if the value is being animated.
            // The value is being animated by the platform itself.
            if (args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation)
            {
                return;
            }

			if (dependencyObject is CompositeTransform transform)
			{
				transform._scale.ScaleY = transform.ScaleY;
			}
		}


		public double SkewX
		{
			get => (double)this.GetValue(SkewXProperty);
			set => this.SetValue(SkewXProperty, value);
		}

		public static readonly DependencyProperty SkewXProperty =
			DependencyProperty.Register("SkewX", typeof(double), typeof(CompositeTransform), new PropertyMetadata(0.0d, OnSkewXChanged));
		private static void OnSkewXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
            // Don't update the internal value if the value is being animated.
            // The value is being animated by the platform itself.
            if (args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation)
            {
                return;
            }

			if (dependencyObject is CompositeTransform transform)
			{
				transform._skew.AngleX = transform.SkewX;
			}
		}

		public double SkewY
		{
			get => (double)this.GetValue(SkewYProperty);
			set => this.SetValue(SkewYProperty, value);
		}

		public static readonly DependencyProperty SkewYProperty =
			DependencyProperty.Register("SkewY", typeof(double), typeof(CompositeTransform), new PropertyMetadata(0.0d, OnSkewYChanged));
		private static void OnSkewYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
            // Don't update the internal value if the value is being animated.
            // The value is being animated by the platform itself.
            if (args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation)
            {
                return;
            }

			if (dependencyObject is CompositeTransform transform)
			{
				transform._skew.AngleY = transform.SkewY;
			}
		}

		public double TranslateX
		{
			get => (double)this.GetValue(TranslateXProperty);
			set => this.SetValue(TranslateXProperty, value);
		}

		public static readonly DependencyProperty TranslateXProperty =
			DependencyProperty.Register("TranslateX", typeof(double), typeof(CompositeTransform), new PropertyMetadata(0.0d, OnTranslateXChanged));
		private static void OnTranslateXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
            // Don't update the internal value if the value is being animated.
            // The value is being animated by the platform itself.
            if (args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation)
            {
                return;
            }

			if (dependencyObject is CompositeTransform transform)
			{
				transform._translation.X = transform.TranslateX;
			}
		}


		public double TranslateY
		{
			get => (double)this.GetValue(TranslateYProperty);
			set => this.SetValue(TranslateYProperty, value);
		}

		public static readonly DependencyProperty TranslateYProperty =
			DependencyProperty.Register("TranslateY", typeof(double), typeof(CompositeTransform), new PropertyMetadata(0.0d, OnTranslateYChanged));
		private static void OnTranslateYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
            // Don't update the internal value if the value is being animated.
            // The value is being animated by the platform itself.
            if (args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation)
            {
                return;
            }

			if (dependencyObject is CompositeTransform transform)
			{
				transform._translation.Y = transform.TranslateY;
			}
		}
	}

}

