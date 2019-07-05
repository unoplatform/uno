#if __ANDROID__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Sensors;

namespace Uno.Devices.Sensors.Helpers
{
	internal class ShakeDetector
	{
		private const int MinimumForce = 10;
		private const int MaxPauseBetweenDirectionChange = 200;
		private const int MinDirectionChange = 3;
		private const int MaxDurationOfShake = 400;

		private readonly Accelerometer _accelerometer;

		private DateTimeOffset _firstDirectionChangeTime = DateTimeOffset.MinValue;
		private DateTimeOffset _lastDirectionChangeTime = DateTimeOffset.MinValue;
		private int _directionChangeCount = 0;
		private double _lastX = 0;
		private double _lastY = 0;
		private double _lastZ = 0;

		public ShakeDetector(Accelerometer accelerometer)
		{
			_accelerometer = accelerometer;
		}

		public void OnSensorChanged(double x, double y, double z, DateTimeOffset now)
		{
			// calculate movement
			var totalMovement = Math.Abs(x + y + z - _lastX - _lastY - _lastZ);

			if (totalMovement > MinimumForce)
			{
				// store first movement time
				if (_firstDirectionChangeTime == DateTimeOffset.MinValue)
				{
					_firstDirectionChangeTime = now;
					_lastDirectionChangeTime = now;
				}

				// check if the last movement was not long ago
				var lastChangeWasAgo = (now - _lastDirectionChangeTime).TotalMilliseconds;
				if (lastChangeWasAgo < MaxPauseBetweenDirectionChange)
				{

					// store movement data
					_lastDirectionChangeTime = now;
					_directionChangeCount++;

					// store last sensor data 
					_lastX = x;
					_lastY = y;
					_lastZ = z;

					// check how many movements are so far
					if (_directionChangeCount >= MinDirectionChange)
					{

						// check total duration
						var totalDuration = (now - _firstDirectionChangeTime).TotalMilliseconds;
						if (totalDuration < MaxDurationOfShake)
						{
							ResetShake();
							_accelerometer.OnShaken(now);
						}
					}

				}
				else
				{
					ResetShake();
				}
			}
		}

		private void ResetShake()
		{
			_firstDirectionChangeTime = DateTimeOffset.MinValue;
			_lastDirectionChangeTime = DateTimeOffset.MinValue;
			_directionChangeCount = 0;
			_lastX = 0;
			_lastY = 0;
			_lastZ = 0;
		}
	}
}
#endif
