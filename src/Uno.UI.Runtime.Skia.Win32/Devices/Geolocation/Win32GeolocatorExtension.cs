#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Windows.Devices.Geolocation;

namespace Uno.UI.Runtime.Skia.Win32.Devices.Geolocation;

/// <summary>
/// Windows implementation of Geolocator using Windows.Devices.Geolocation WinRT APIs.
/// This is a stub implementation that throws NotImplementedException.
/// A full implementation would use WinRT APIs or Windows Location API.
/// </summary>
internal class Win32GeolocatorExtension : IGeolocatorExtension
{
	public Task<GeolocationAccessStatus> RequestAccessAsync()
	{
		// TODO: Implement Windows Geolocation API integration
		// This could use:
		// 1. Windows.Devices.Geolocation WinRT APIs (if available in the process)
		// 2. Windows Location API via P/Invoke
		// 3. Windows.Services.Maps for geolocation
		return Task.FromResult(GeolocationAccessStatus.Unspecified);
	}

	public Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters)
	{
		// TODO: Implement Windows Geolocation API integration
		throw new NotImplementedException("Geolocation is not yet fully implemented on Windows Skia. The WinRT or native Windows Location API integration needs to be added.");
	}

	public Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters, TimeSpan maximumAge, TimeSpan timeout)
	{
		// TODO: Implement Windows Geolocation API integration
		throw new NotImplementedException("Geolocation is not yet fully implemented on Windows Skia. The WinRT or native Windows Location API integration needs to be added.");
	}

	public void StartPositionChanged(uint desiredAccuracyInMeters, Action<Geoposition> onPositionChanged)
	{
		// TODO: Implement Windows Geolocation API integration
		// For now, this is a no-op
	}

	public void StopPositionChanged()
	{
		// TODO: Implement Windows Geolocation API integration
		// For now, this is a no-op
	}
}

internal static class Win32GeolocatorExtensionRegistrar
{
	[Uno.Foundation.Extensibility.ModuleInitializer]
	internal static void RegisterExtension()
	{
		ApiExtensibility.Register(typeof(Geolocator), o => new Win32GeolocatorExtension());
	}
}
