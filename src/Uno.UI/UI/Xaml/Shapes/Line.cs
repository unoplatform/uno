#if !__IOS__ && !__MACOS__
#define LEGACY_SHAPE_MEASURE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Line
#if LEGACY_SHAPE_MEASURE
		: ArbitraryShapeBase
#endif
	{
		public Line()
		{
			InitializePartial();
		}

		partial void InitializePartial();

		#region X1 (DP)
		public double X1
		{
			get => (double)GetValue(X1Property);
			set => SetValue(X1Property, value);
		}

		public static DependencyProperty X1Property { get; } = DependencyProperty.Register(
			"X1",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
#if LEGACY_SHAPE_MEASURE
				, propertyChangedCallback: OnX1PropertyChanged
#endif
			)
		);
		#endregion

		#region X2 (DP)
		public double X2
		{
			get => (double)GetValue(X2Property);
			set => SetValue(X2Property, value);
		}

		public static DependencyProperty X2Property { get; } = DependencyProperty.Register(
			"X2",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
#if LEGACY_SHAPE_MEASURE
				, propertyChangedCallback: OnX2PropertyChanged
#endif
			)
		);
		#endregion

		#region Y1 (DP)
		public double Y1
		{
			get => (double)GetValue(Y1Property);
			set => SetValue(Y1Property, value);
		}

		public static DependencyProperty Y1Property { get; } = DependencyProperty.Register(
			"Y1",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
#if LEGACY_SHAPE_MEASURE
				, propertyChangedCallback: OnY1PropertyChanged
#endif
			)
		);
		#endregion

		#region Y2 (DP)
		public double Y2
		{
			get => (double)GetValue(Y2Property);
			set => SetValue(Y2Property, value);
		}

		public static DependencyProperty Y2Property { get; } = DependencyProperty.Register(
			"Y2",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
#if LEGACY_SHAPE_MEASURE
				, propertyChangedCallback: OnY2PropertyChanged
#endif
			)
		);
		#endregion

#if LEGACY_SHAPE_MEASURE

		private static void OnX1PropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is Line line)
			{
				line.OnX1PropertyChangedPartial((double)args.OldValue, (double)args.NewValue);
			}
		}
		partial void OnX1PropertyChangedPartial(double oldValue, double newValue);

		private static void OnX2PropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is Line line)
			{
				line.OnX2PropertyChangedPartial((double)args.OldValue, (double)args.NewValue);
			}
		}
		partial void OnX2PropertyChangedPartial(double oldValue, double newValue);

		private static void OnY1PropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is Line line)
			{
				line.OnY1PropertyChangedPartial((double)args.OldValue, (double)args.NewValue);
			}
		}

		partial void OnY1PropertyChangedPartial(double oldValue, double newValue);

		private static void OnY2PropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is Line line)
			{
				line.OnY2PropertyChangedPartial((double)args.OldValue, (double)args.NewValue);
			}
		}

		partial void OnY2PropertyChangedPartial(double oldValue, double newValue);

		protected internal override IEnumerable<object> GetShapeParameters()
		{
			yield return X1;
			yield return X2;
			yield return Y1;
			yield return Y2;

			foreach (var p in base.GetShapeParameters())
			{
				yield return p;
			}
		}
#endif
	}
}
