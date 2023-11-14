using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml.Shapes
{
	public abstract partial class Shape : FrameworkElement
	{
		private const double DefaultStrokeThicknessWhenNoStrokeDefined = 0.0;

		private Action _brushChanged;
		private Action _strokeBrushChanged;

		/// <summary>
		/// Returns 0.0 if Stroke is <c>null</c>, otherwise, StrokeThickness
		/// </summary>
		/// <remarks>Path does not need to define a stroke, in that case StrokeThickness should just return 0.
		/// Other shapes like Ellipse and Polygon will not draw if Stroke is null so returning 0 will have no effect
		///</remarks>
		private protected double ActualStrokeThickness => Stroke == null
			? DefaultStrokeThicknessWhenNoStrokeDefined
			: LayoutRound(StrokeThickness);

		#region Fill Dependency Property
		//This field is never accessed. It just exists to create a reference, because the DP causes issues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
		private Brush _fillStrongref;
		public Brush Fill
		{
			get => (Brush)this.GetValue(FillProperty);
			set
			{
				this.SetValue(FillProperty, value);
				_fillStrongref = value;
			}
		}

		public static DependencyProperty FillProperty { get; } = DependencyProperty.Register(
			"Fill",
			typeof(Brush),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: SolidColorBrushHelper.Transparent,
				options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild,
				propertyChangedCallback: (s, e) => ((Shape)s).OnFillChanged((Brush)e.OldValue, (Brush)e.NewValue)
			)
		);

		private void OnFillChanged(Brush oldValue, Brush newValue)
		{
			Brush.SetupBrushChanged(oldValue, newValue, ref _brushChanged, () => OnFillBrushChanged());
		}

		#endregion

		#region Stroke Dependency Property
		public Brush Stroke
		{
			get => (Brush)this.GetValue(StrokeProperty);
			set => this.SetValue(StrokeProperty, value);
		}

		public static DependencyProperty StrokeProperty { get; } = DependencyProperty.Register(
			"Stroke",
			typeof(Brush),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsArrange,
				propertyChangedCallback: (s, e) => ((Shape)s).OnStrokeChanged((Brush)e.OldValue, (Brush)e.NewValue)
			)
		);

		private void OnStrokeChanged(Brush oldValue, Brush newValue)
		{
			if ((oldValue is null) ^ (newValue is null))
			{
				// Moving from null to non-null or vice-versa affects measure.
				InvalidateMeasure();
			}

			Brush.SetupBrushChanged(oldValue, newValue, ref _strokeBrushChanged, () => OnStrokeBrushChanged());
		}

		#endregion

		#region StrokeThickness Dependency Property
		public double StrokeThickness
		{
			get => (double)this.GetValue(StrokeThicknessProperty);
			set => this.SetValue(StrokeThicknessProperty, value);
		}

		public static DependencyProperty StrokeThicknessProperty { get; } = DependencyProperty.Register(
			nameof(StrokeThickness),
			typeof(double),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: 1.0d,
				propertyChangedCallback: (s, e) => ((Shape)s).OnStrokeThicknessChanged()
			)
		);

		private void OnStrokeThicknessChanged()
		{
			if (Stroke is not null)
			{
				// Changing stroke thickness will only have effect if Stroke is not null.
				InvalidateMeasure();
			}
		}
		#endregion

		#region Stretch Dependency Property
		public Stretch Stretch
		{
			get => (Stretch)this.GetValue(StretchProperty);
			set => this.SetValue(StretchProperty, value);
		}

		public static DependencyProperty StretchProperty { get; } = DependencyProperty.Register(
			"Stretch",
			typeof(Stretch),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: Stretch.None, // Note: this is overriden in ctor for Rectangle and Ellipse
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);
		#endregion

		#region StrokeDashArray Dependency Property
		public DoubleCollection StrokeDashArray
		{
			get => (DoubleCollection)this.GetValue(StrokeDashArrayProperty);
			set => this.SetValue(StrokeDashArrayProperty, value);
		}

		public static DependencyProperty StrokeDashArrayProperty { get; } = DependencyProperty.Register(
			"StrokeDashArray",
			typeof(DoubleCollection),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);
		#endregion

		internal override bool IsViewHit()
			=> Fill != null; // Do not invoke base.IsViewHit(): We don't have to have de FrameworkElement.Background to be hit testable!

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
		}
	}
}
