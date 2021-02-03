
namespace Windows.UI.Xaml.Controls.Maps
{
	public partial class MapInputEventArgs : DependencyObject
	{
		public Windows.Devices.Geolocation.Geopoint Location { get; }
		public Windows.Foundation.Point Position { get; }

		public MapInputEventArgs() : base() => new MapInputEventArgs(null, new Windows.Foundation.Point());

		internal MapInputEventArgs(Windows.Devices.Geolocation.Geopoint location, Windows.Foundation.Point position) : base()
        {
			Location = location;
			Position = position;
		}
	}
}
