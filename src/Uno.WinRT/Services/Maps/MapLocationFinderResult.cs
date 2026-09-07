using System.Collections.Generic;

namespace Windows.Services.Maps
{
	public sealed partial class MapLocationFinderResult
	{
		internal MapLocationFinderResult(
			IReadOnlyList<MapLocation> locations,
			MapLocationFinderStatus status
		)
		{
			Locations = locations;
			Status = status;
		}

		public IReadOnlyList<MapLocation> Locations { get; }
		public MapLocationFinderStatus Status { get; }
	}
}
