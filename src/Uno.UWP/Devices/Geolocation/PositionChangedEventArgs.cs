#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	public  partial class PositionChangedEventArgs 
	{
		internal PositionChangedEventArgs(Geoposition position) =>
			Position = position;

		public Geoposition Position { get; }
	}
}
