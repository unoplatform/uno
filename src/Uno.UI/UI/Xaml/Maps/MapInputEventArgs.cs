
namespace Windows.UI.Xaml.Controls.Maps
{
	public partial class MapInputEventArgs : DependencyObject
	{
		public Windows.Devices.Geolocation.Geopoint Location { get; }
		public Windows.Foundation.Point Position { get; }

		public MapInputEventArgs()
		{
		}
	}
}
