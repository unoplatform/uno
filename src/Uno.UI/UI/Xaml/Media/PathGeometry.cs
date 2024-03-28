using Uno.Media;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(Figures))]
	public partial class PathGeometry : Geometry
	{
		public PathGeometry()
		{
			// This is done here to ensure that the Parent is set properly on the new PathFigureCollection.
			Figures = new PathFigureCollection();

			InitPartials();
		}

		partial void InitPartials();

		#region FillRule

		public static DependencyProperty FillRuleProperty { get; } =
			DependencyProperty.Register(
				"FillRule",
				typeof(FillRule),
				typeof(PathGeometry),
				new FrameworkPropertyMetadata(
					defaultValue: FillRule.EvenOdd,
					options: FrameworkPropertyMetadataOptions.AffectsRender
				)
			);

		public FillRule FillRule
		{
			get => (FillRule)this.GetValue(FillRuleProperty);
			set => this.SetValue(FillRuleProperty, value);
		}

		#endregion

		#region Figures

		public PathFigureCollection Figures
		{
			get => (PathFigureCollection)this.GetValue(FiguresProperty);
			set => this.SetValue(FiguresProperty, value);
		}

		public static DependencyProperty FiguresProperty { get; } =
			DependencyProperty.Register(
				"Figures",
				typeof(PathFigureCollection),
				typeof(PathGeometry),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
