using System;
using System.Threading.Tasks;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Uno.Devices.Sensors.Helpers;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents the hinge angle sensor in a dual-screen device.
	/// </summary>
	public partial class HingeAngleSensor
	{
		private static readonly object _syncLock = new object();

		private static bool _initializationAttempted;
		private static HingeAngleSensor? _instance;
		private static INativeHingeAngleSensor? _hingeAngleSensor;

		private TypedEventHandler<HingeAngleSensor, HingeAngleSensorReadingChangedEventArgs>? _readingChanged;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private HingeAngleSensor()
		{
		}

		/// <summary>
		/// Asynchronously retrieves the default hinge angle sensor.
		/// </summary>
		/// <returns>When this method completes, it returns a reference to the default HingeAngleSensor.</returns>
		public static IAsyncOperation<HingeAngleSensor?> GetDefaultAsync()
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

		/// <summary>
		/// Occurs when the hinge angle sensor in a dual-screen device reports a change in opening angle.
		/// </summary>
		public event TypedEventHandler<HingeAngleSensor, HingeAngleSensorReadingChangedEventArgs> ReadingChanged
		{
			add
			{
				lock (_syncLock)
				{
					var isFirstSubscriber = _readingChanged == null;
					_readingChanged += value;
					if (isFirstSubscriber)
					{
						StartReading();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_readingChanged -= value;
					if (_readingChanged == null)
					{
						StopReading();
					}
				}
			}
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
					owner.Log().Warn("You need to reference Uno.UI.Foldable NuGet package from your project to use this feature.");
				}
			}
		}

		private void StartReading() =>
			_hingeAngleSensor!.ReadingChanged += OnNativeReadingChanged;

		private void StopReading() =>
			_hingeAngleSensor!.ReadingChanged -= OnNativeReadingChanged;

		private void OnNativeReadingChanged(object? sender, NativeHingeAngleReading e) =>
			_readingChanged?.Invoke(this, new HingeAngleSensorReadingChangedEventArgs(new HingeAngleReading(e.AngleInDegrees, e.Timestamp)));
	}
}
