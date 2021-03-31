#nullable enable

using System;

namespace Windows.Devices.Geolocation.Geofencing
{
	public partial class Geofence
	{
		public TimeSpan Duration { get; }
		public TimeSpan DwellTime { get; }
		public IGeoshape Geoshape { get; }
		public string Id { get; internal set; }
		public MonitoredGeofenceStates MonitoredStates { get; }
		public bool SingleUse { get; }
		public DateTimeOffset StartTime { get; }

		public Geofence(string id, IGeoshape geoshape)
			: this(id, geoshape, MonitoredGeofenceStates.Entered | MonitoredGeofenceStates.Exited, false)
		{
		}

		public Geofence(string id, IGeoshape geoshape, MonitoredGeofenceStates monitoredStates, bool singleUse)
			: this(id, geoshape, monitoredStates, singleUse, TimeSpan.FromSeconds(10))
		{
		}

		public Geofence(string id, IGeoshape geoshape, MonitoredGeofenceStates monitoredStates, bool singleUse, TimeSpan dwellTime)
		: this(id, geoshape, monitoredStates, singleUse, dwellTime, new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.FromSeconds(0)), TimeSpan.FromSeconds(0))
		{
		}

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
