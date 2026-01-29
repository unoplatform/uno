#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Windows.Devices.Geolocation;

namespace Uno.UI.Runtime.Skia.MacOS.Devices.Geolocation;

/// <summary>
/// macOS implementation of Geolocator.
/// This is a stub implementation that throws NotImplementedException.
/// A full implementation would use CoreLocation via P/Invoke to the native UnoNativeMac library.
/// </summary>
internal class MacOSGeolocatorExtension : IGeolocatorExtension
{
	public static void Register()
	{
		ApiExtensibility.Register(typeof(Geolocator), o => new MacOSGeolocatorExtension());
	}

	public Task<GeolocationAccessStatus> RequestAccessAsync()
	{
		// TODO: Implement CoreLocation integration via UnoNativeMac native library
		// This requires:
		// 1. Adding Objective-C code to UnoNativeMac for CLLocationManager
		// 2. Implementing P/Invoke methods to call into the native code
		// 3. Handling authorization status callbacks
		return Task.FromResult(GeolocationAccessStatus.Unspecified);
	}

	public Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters)
	{
		// TODO: Implement CoreLocation integration via UnoNativeMac native library
		throw new NotImplementedException("Geolocation is not yet fully implemented on macOS Skia. The native CoreLocation integration needs to be added to the UnoNativeMac library.");
	}

	public Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters, TimeSpan maximumAge, TimeSpan timeout)
	{
		// TODO: Implement CoreLocation integration via UnoNativeMac native library
		throw new NotImplementedException("Geolocation is not yet fully implemented on macOS Skia. The native CoreLocation integration needs to be added to the UnoNativeMac library.");
	}

	public void StartPositionChanged(uint desiredAccuracyInMeters, Action<Geoposition> onPositionChanged)
	{
		// TODO: Implement CoreLocation integration via UnoNativeMac native library
		// For now, this is a no-op
	}

	public void StopPositionChanged()
	{
		// TODO: Implement CoreLocation integration via UnoNativeMac native library
		// For now, this is a no-op
	}
}
