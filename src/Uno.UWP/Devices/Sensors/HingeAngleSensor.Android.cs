using System;
using System.Threading.Tasks;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Uno.Logging;
using Uno.Extensions;
using Uno.Devices.Sensors.Helpers;
using Uno.Helpers;

namespace Windows.Devices.Sensors
{
	public partial class HingeAngleSensor
	{
		private static readonly object _syncLock = new object();

		private static bool _initializationAttempted = false;
		private static HingeAngleSensor _instance;
		private static INativeHingeAngleSensor _hingeAngleSensor = null;

		private StartStopEventWrapper<TypedEventHandler<HingeAngleSensor, HingeAngleSensorReadingChangedEventArgs>> _readingChanged;

		private HingeAngleSensor()
		{
			_readingChanged = new StartStopEventWrapper<TypedEventHandler<HingeAngleSensor, HingeAngleSensorReadingChangedEventArgs>>(
				() => StartReading(),
				() => StopReading(),
				_syncLock);
		}

		public static IAsyncOperation<HingeAngleSensor> GetDefaultAsync()
		{
			// avoid locking if possible
			if (!_initializationAttempted)
			{
				lock (_syncLock)
				{
					if (!_initializationAttempted) //double-check for race conditions
					{
						// we need to create a dummy instance to give IUnoHingeAngleSensor an owner
						var sensor = new HingeAngleSensor();
						TryInitializeHingeAngleSensor(sensor);
						if (_hingeAngleSensor?.DeviceHasHinge == true)
						{
							_instance = sensor;
						}
						_initializationAttempted = true;
					}
				}
			}
			return Task.FromResult(_instance).AsAsyncOperation();
		}

		public event TypedEventHandler<HingeAngleSensor, HingeAngleSensorReadingChangedEventArgs> ReadingChanged
		{
			add => _readingChanged.AddHandler(value);
			remove => _readingChanged.RemoveHandler(value);
		}

		private static void TryInitializeHingeAngleSensor(HingeAngleSensor owner)
		{
			if (_hingeAngleSensor != null)
			{
				return;
			}
			lock (_syncLock)
			{
				if (_hingeAngleSensor == null && !ApiExtensibility.CreateInstance(owner, out _hingeAngleSensor))
				{
					owner.Log().Warn("You need to reference Uno.UI.DualScreen NuGet package from your project to use this feature.");
				}
			}
		}

		private void StartReading() =>
			_hingeAngleSensor.ReadingChanged += OnNativeReadingChanged;

		private void StopReading() =>
			_hingeAngleSensor.ReadingChanged -= OnNativeReadingChanged;

		private void OnNativeReadingChanged(object sender, NativeHingeAngleReading e) =>
			_readingChanged.Event?.Invoke(this, new HingeAngleSensorReadingChangedEventArgs(new HingeAngleReading(e.AngleInDegrees, e.Timestamp)));
	}
}
