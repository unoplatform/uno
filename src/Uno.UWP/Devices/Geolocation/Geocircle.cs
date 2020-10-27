namespace Windows.Devices.Geolocation
{
	public partial class Geocircle : IGeoshape
	{
		public BasicGeoposition Center { get; }
		public double Radius { get; }

		public AltitudeReferenceSystem AltitudeReferenceSystem { get; }
		public GeoshapeType GeoshapeType { get; }
		public uint SpatialReferenceId { get; }
		public Geocircle(BasicGeoposition position, double radius)
		{
			Center = position;
			Radius = radius;
			AltitudeReferenceSystem = AltitudeReferenceSystem.Unspecified;
			GeoshapeType = GeoshapeType.Geocircle;
			SpatialReferenceId = 4326;
		}
	}
}
