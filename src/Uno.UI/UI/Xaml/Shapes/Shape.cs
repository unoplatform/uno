using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	public abstract partial class Shape : FrameworkElement
	{
		private const double DefaultStrokeThicknessWhenNoStrokeDefined = 0.0;

		private readonly SerialDisposable _brushChanged = new SerialDisposable();

		/// <summary>
		/// Returns StrokeThickness or 0.0 if Stroke is <c>null</c>
		/// </summary>
		private protected double ActualStrokeThickness
		{
			//Path does not need to define a stroke, in that case StrokeThickness should just return 0
			//Other shapes like Ellipse and Polygon will not draw if Stroke is null so returning 0 will have no effect
			get => Stroke == null ? DefaultStrokeThicknessWhenNoStrokeDefined : StrokeThickness;
		}

		#region Fill Dependency Property

		//This field is never accessed. It just exists to create a reference, because the DP causes issues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
		private Brush _fillStrongref;
		public Brush Fill
		{
			get { return (Brush)this.GetValue(FillProperty); }
			set
			{
				this.SetValue(FillProperty, value);
				_fillStrongref = value;
			}
		}

		public static readonly DependencyProperty FillProperty =
			DependencyProperty.Register(
				"Fill",
				typeof(Brush),
				typeof(Shape),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Transparent,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
					propertyChangedCallback: (s, e) => ((Shape)s).OnFillChanged((Brush)e.NewValue)
				)
			);

		#endregion

		#region Stroke Dependency Property
		public Brush Stroke
		{
			get { return (Brush)this.GetValue(StrokeProperty); }
			set { this.SetValue(StrokeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StrokeProperty =
			DependencyProperty.Register("Stroke", typeof(Brush), typeof(Shape), new PropertyMetadata(null, (s, e) =>
				((Shape)s).OnStrokeUpdated((Brush)e.NewValue)
			));

		#endregion

		#region StrokeThickness Dependency Property
		public double StrokeThickness
		{
			get { return (double)this.GetValue(StrokeThicknessProperty); }
			set { this.SetValue(StrokeThicknessProperty, value); }
		}

		public static readonly DependencyProperty StrokeThicknessProperty =
			DependencyProperty.Register(
				nameof(StrokeThickness),
				typeof(double),
				typeof(Shape),
				new FrameworkPropertyMetadata(
					defaultValue: 1.0d,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((Shape)s).OnStrokeThicknessUpdated((double)e.NewValue)
			)
		);

		#endregion

		#region Stretch Dependency Property
		public Stretch Stretch
		{
			get { return (Stretch)this.GetValue(StretchProperty); }
			set { this.SetValue(StretchProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Stretch.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StretchProperty =
			DependencyProperty.Register("Stretch", typeof(Stretch), typeof(Shape), new PropertyMetadata(Stretch.None, (s, e) =>
				((Shape)s).OnStretchUpdated((Stretch)e.NewValue)
			));

		#endregion

		#region StrokeDashArray Dependency Property
		public DoubleCollection StrokeDashArray
		{
			get { return (DoubleCollection)this.GetValue(StrokeDashArrayProperty); }
			set { this.SetValue(StrokeDashArrayProperty, value); }
		}

		// Using a DependencyProperty as the backing store for StrokeDashArray.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StrokeDashArrayProperty =
			DependencyProperty.Register("StrokeDashArray", typeof(DoubleCollection), typeof(Shape), new PropertyMetadata(null, (s, e) =>
				((Shape)s).OnStrokeDashArrayUpdated((DoubleCollection)e.NewValue)
			));

		#endregion

		protected virtual void OnFillChanged(Brush newValue)
		{
			_brushChanged.Disposable = Brush.AssignAndObserveBrush(newValue, _ =>
#if __WASM__
				OnFillUpdatedPartial()
#else
				RefreshShape(true)
#endif
			);

			OnFillUpdated(newValue);
		}

		protected virtual void OnFillUpdated(Brush newValue)
		{
			OnFillUpdatedPartial();
			RefreshShape();
		}
		partial void OnFillUpdatedPartial();

		protected virtual void OnStrokeUpdated(Brush newValue)
		{
			OnStrokeUpdatedPartial();
			RefreshShape();
		}
		partial void OnStrokeUpdatedPartial();

		protected virtual void OnStretchUpdated(Stretch newValue)
		{
			OnStretchUpdatedPartial();
			RefreshShape();
		}
		partial void OnStretchUpdatedPartial();

		protected virtual void OnStrokeThicknessUpdated(double newValue)
		{
			OnStrokeThicknessUpdatedPartial();
			RefreshShape();
		}
		partial void OnStrokeThicknessUpdatedPartial();

		protected virtual void OnStrokeDashArrayUpdated(DoubleCollection newValue)
		{
			OnStrokeDashArrayUpdatedPartial();
			RefreshShape();
		}
		partial void OnStrokeDashArrayUpdatedPartial();

		protected virtual void RefreshShape(bool forceRefresh = false) { }

		internal override bool IsViewHit()
			=> Fill != null; // Do not invoke base.IsViewHit(): We don't have to have de FramewokrElement.Background to be hit testable!
	}
}
