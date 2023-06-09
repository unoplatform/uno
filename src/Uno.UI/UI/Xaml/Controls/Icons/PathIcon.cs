using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a vector path as its content.
/// </summary>
public partial class PathIcon : IconElement
{
	private readonly Path _path;

	/// <summary>
	/// Initializes a new instance of the PathIcon class.
	/// </summary>
	public PathIcon()
	{
		_path = new Path();
		AddIconChild(_path);

		SynchronizeProperties();
	}

	/// <summary>
	/// Gets or sets a Geometry that specifies the shape to be drawn. In XAML. this can also be set using a string that describes Move and draw commands syntax.
	/// </summary>
	public Geometry Data
	{
		get => (Geometry)GetValue(DataProperty);
		set => SetValue(DataProperty, value);
	}

	/// <summary>
	/// Identifies the Data dependency property.
	/// </summary>
	public static DependencyProperty DataProperty { get; } =
		DependencyProperty.Register(
			nameof(Data),
			typeof(Geometry),
			typeof(PathIcon),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: (s, e) => ((PathIcon)s)._path.Data = (Geometry)e.NewValue));

	private void SynchronizeProperties()
	{
		_path.HorizontalAlignment = HorizontalAlignment.Stretch;
		_path.VerticalAlignment = VerticalAlignment.Stretch;

		_path.Fill = Foreground;
		_path.Data = Data;
	}

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
	{
		// This may occur while executing the base constructor
		// so _path may still be null.
		if (_path is not null)
		{
			_path.Fill = (Brush)e.NewValue;
		}
	}
}
