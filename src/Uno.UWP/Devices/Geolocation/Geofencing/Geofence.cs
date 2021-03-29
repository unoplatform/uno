#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation.Geofencing
{
	public partial class Geofence
	{
		public TimeSpan Duration { get; internal set; }
		public TimeSpan DwellTime { get; internal set; }
		public IGeoshape Geoshape { get; internal set; }
		public string Id { get; internal set; }
		public MonitoredGeofenceStates MonitoredStates { get; internal set; }
		public bool SingleUse { get; internal set; }
		public DateTimeOffset StartTime { get; internal set; }

		public Geofence(string id, IGeoshape geoshape)
			=> Geofence(id, geoshape, MonitoredGeofenceStates.Entered | MonitoredGeofenceStates.Exited, false);

		public Geofence(string id, IGeoshape geoshape, MonitoredGeofenceStates monitoredStates, bool singleUse)
			=> Geofence(id, geoshape, monitoredStates, singleUse, TimeSpan.FromSeconds(10));

		public Geofence(string id, IGeoshape geoshape, MonitoredGeofenceStates monitoredStates, bool singleUse, TimeSpan dwellTime)
		=> Geofence(id, geoshape, monitoredStates, singleUse, dwellTime, new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.FromSeconds(0)), TimeSpan.FromSeconds(0));

		public Geofence(string id, IGeoshape geoshape, MonitoredGeofenceStates monitoredStates, bool singleUse, TimeSpan dwellTime, DateTimeOffset startTime, TimeSpan duration)
		{
			Duration = duration;
			DwellTime = dwellTime;
			Geoshape = geoshape;
			Id = id;
			MonitoredStates = monitoredStates;
			SingleUse = singleUse;
			StartTime = startTime;
		}
	}
}
