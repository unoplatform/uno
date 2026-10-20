using System;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a gyrometer reading.
	/// </summary>
	public partial class GyrometerReading
	{
		internal GyrometerReading(
			double angularVelocityX,
			double angularVelocityY,
			double angularVelocityZ,
			DateTimeOffset timestamp)
		{
			AngularVelocityX = angularVelocityX;
			AngularVelocityY = angularVelocityY;
			AngularVelocityZ = angularVelocityZ;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the angular velocity, in degrees per second, about the x-axis.
		/// </summary>
		public double AngularVelocityX { get; }

		/// <summary>
		/// Gets the angular velocity, in degrees per second, about the y-axis.
		/// </summary>
		public double AngularVelocityY { get; }

		/// <summary>
		/// Gets the angular velocity, in degrees per second, about the z-axis.
		/// </summary>
		public double AngularVelocityZ { get; }

		/// <summary>
		/// Gets the time at which the sensor reported the reading.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
	}
}
