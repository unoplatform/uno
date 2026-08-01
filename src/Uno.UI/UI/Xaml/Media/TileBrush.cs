namespace Microsoft.UI.Xaml.Media;

public partial class TileBrush : Brush
{
	public static DependencyProperty AlignmentXProperty { get; } =
		DependencyProperty.Register(nameof(AlignmentX), typeof(AlignmentX), typeof(TileBrush), new FrameworkPropertyMetadata(AlignmentX.Center));

	public AlignmentX AlignmentX
	{
		get => (AlignmentX)GetValue(AlignmentXProperty);
		set => SetValue(AlignmentXProperty, value);
	}

	public static DependencyProperty AlignmentYProperty { get; } =
		DependencyProperty.Register(nameof(AlignmentY), typeof(AlignmentY), typeof(TileBrush), new FrameworkPropertyMetadata(AlignmentY.Center));

	public AlignmentY AlignmentY
	{
		get => (AlignmentY)GetValue(AlignmentYProperty);
		set => SetValue(AlignmentYProperty, value);
	}

	public static DependencyProperty StretchProperty { get; } =
		DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(TileBrush), new FrameworkPropertyMetadata(Stretch.Fill));

	public Stretch Stretch
	{
		get => (Stretch)GetValue(StretchProperty);
		set => SetValue(StretchProperty, value);
	}

	protected TileBrush()
	{
	}
}
