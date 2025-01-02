using Windows.Devices.Geolocation;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Maps;

public partial class MapInputEventArgs : DependencyObject
{
	public Geopoint Location { get; }
	public Point Position { get; }

	public MapInputEventArgs()
	{
	}

	internal MapInputEventArgs(Geopoint location, Point position)
	{
		Location = location;
		Position = position;
	}
}
