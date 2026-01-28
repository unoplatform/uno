#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Windows.Devices.Geolocation;

namespace Uno.UI.Runtime.Skia.X11.Devices.Geolocation;

/// <summary>
/// Linux implementation of Geolocator using GeoClue2 D-Bus API.
/// This is a stub implementation that can be extended to use GeoClue2.
/// </summary>
internal class X11GeolocatorExtension : IGeolocatorExtension
{
	public Task<GeolocationAccessStatus> RequestAccessAsync()
	{
		// TODO: Implement GeoClue2 D-Bus API integration
		// For now, return Unspecified to indicate the feature is not yet implemented
		return Task.FromResult(GeolocationAccessStatus.Unspecified);
	}

	public Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters)
	{
		// TODO: Implement GeoClue2 D-Bus API integration
		throw new NotImplementedException("Geolocation is not yet implemented on Linux. Consider using GeoClue2 D-Bus API for implementation.");
	}

	public Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters, TimeSpan maximumAge, TimeSpan timeout)
	{
		// TODO: Implement GeoClue2 D-Bus API integration
		throw new NotImplementedException("Geolocation is not yet implemented on Linux. Consider using GeoClue2 D-Bus API for implementation.");
	}

	public void StartPositionChanged(uint desiredAccuracyInMeters, Action<Geoposition> onPositionChanged)
	{
		// TODO: Implement GeoClue2 D-Bus API integration
		// For now, this is a no-op
	}

	public void StopPositionChanged()
	{
		// TODO: Implement GeoClue2 D-Bus API integration
		// For now, this is a no-op
	}
}

internal static class X11GeolocatorExtensionRegistrar
{
	[Uno.Foundation.Extensibility.ModuleInitializer]
	internal static void RegisterExtension()
	{
		ApiExtensibility.Register(typeof(Geolocator), o => new X11GeolocatorExtension());
	}
}
