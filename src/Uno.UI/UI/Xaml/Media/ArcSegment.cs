using System;
using System.Linq;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Media
{
	public partial class ArcSegment : PathSegment
	{
		#region SweepDirection

		public SweepDirection SweepDirection
		{
			get => (SweepDirection)this.GetValue(SweepDirectionProperty);
			set => this.SetValue(SweepDirectionProperty, value);
		}

		public static DependencyProperty SweepDirectionProperty { get; } =
			DependencyProperty.Register(
				"SweepDirection",
				typeof(SweepDirection),
				typeof(ArcSegment),
				new FrameworkPropertyMetadata(
					defaultValue: SweepDirection.Counterclockwise,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region Size

		public Foundation.Size Size
		{
			get => (Foundation.Size)this.GetValue(SizeProperty);
			set => this.SetValue(SizeProperty, value);
		}

		public static DependencyProperty SizeProperty { get; } =
			DependencyProperty.Register(
				"Size",
				typeof(Foundation.Size),
				typeof(ArcSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Foundation.Size(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region RotationAngle

		public double RotationAngle
		{
			get => (double)this.GetValue(RotationAngleProperty);
			set => this.SetValue(RotationAngleProperty, value);
		}

		public static DependencyProperty RotationAngleProperty { get; } =
			DependencyProperty.Register(
				"RotationAngle",
				typeof(double),
				typeof(ArcSegment),
				new FrameworkPropertyMetadata(
					defaultValue: 0.0,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region Point

		public Foundation.Point Point
		{
			get => (Foundation.Point)this.GetValue(PointProperty);
			set => this.SetValue(PointProperty, value);
		}

		public static DependencyProperty PointProperty { get; } =
			DependencyProperty.Register(
				"Point",
				typeof(Foundation.Point),
				typeof(ArcSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Foundation.Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region IsLargeArc

		public bool IsLargeArc
		{
			get => (bool)this.GetValue(IsLargeArcProperty);
			set => this.SetValue(IsLargeArcProperty, value);
		}

		public static DependencyProperty IsLargeArcProperty { get; } =
			DependencyProperty.Register(
				"IsLargeArc",
				typeof(bool),
				typeof(ArcSegment),
				new FrameworkPropertyMetadata(
					defaultValue: false,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
