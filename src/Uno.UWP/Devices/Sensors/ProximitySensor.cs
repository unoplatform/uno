using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Sensors;

/// <summary>
/// Provides an interface for a proximity sensor to determine whether or not an object is detected.
/// </summary>
public partial class ProximitySensor
{
	internal const string InterfaceId = "{5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D}";

	private readonly StartStopEventWrapper<TypedEventHandler<ProximitySensor, ProximitySensorReadingChangedEventArgs>> _readingChangedWrapper;

	private ProximitySensor(string deviceId)
	{
		_readingChangedWrapper = new(() => StartReading(), () => StopReading());

		DeviceId = deviceId;
	}

	partial void StartReading();

	partial void StopReading();

	/// <summary>
	/// Gets the device selector.
	/// </summary>
	/// <returns></returns>
	public static string GetDeviceSelector() =>
		$"System.Devices.InterfaceClassGuid:=\"{InterfaceId}\" AND " +
		"System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

	/// <summary>
	/// Occurs each time the proximity sensor reports a new value.
	/// </summary>
	public event TypedEventHandler<ProximitySensor, ProximitySensorReadingChangedEventArgs> ReadingChanged
	{
		add => _readingChangedWrapper.AddHandler(value);
		remove => _readingChangedWrapper.RemoveHandler(value);
	}
}
