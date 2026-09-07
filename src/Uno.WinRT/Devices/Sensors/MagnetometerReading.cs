using System;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Represents a magnetometer reading.
	/// </summary>
	public partial class MagnetometerReading
	{
		internal MagnetometerReading(
			float magneticFieldX,
			float magneticFieldY,
			float magneticFieldZ,
			MagnetometerAccuracy directionalAccuracy,
			DateTimeOffset timestamp)
		{
			MagneticFieldX = magneticFieldX;
			MagneticFieldY = magneticFieldY;
			MagneticFieldZ = magneticFieldZ;
			DirectionalAccuracy = directionalAccuracy;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the magnetometer's directional accuracy.
		/// </summary>
		public global::Windows.Devices.Sensors.MagnetometerAccuracy DirectionalAccuracy { get; }

		/// <summary>
		/// Gets the magnetic field strength in microteslas along the X axis.
		/// </summary>
		public float MagneticFieldX { get; }

		/// <summary>
		/// Gets the magnetic field strength in microteslas along the Y axis.
		/// </summary>
		public float MagneticFieldY { get; }

		/// <summary>
		/// Gets the magnetic field strength in microteslas along the Z axis.
		/// </summary>
		public float MagneticFieldZ { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
