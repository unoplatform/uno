using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Indicates the orientation of the device.
	/// </summary>
	public enum SimpleOrientation
	{
		/// <summary>
		/// The device is not rotated.
		/// </summary>
		NotRotated = 0,

		/// <summary>
		/// The device is rotated 90-degrees counter-clockwise.
		/// </summary>
		Rotated90DegreesCounterclockwise = 1,

		/// <summary>
		/// The device is rotated 180-degrees counter-clockwise.
		/// </summary>
		Rotated180DegreesCounterclockwise = 2,

		/// <summary>
		/// The device is rotated 270-degrees counter-clockwise.
		/// </summary>
		Rotated270DegreesCounterclockwise = 3,

		/// <summary>
		/// The device is face-up and the display is visible to the user.
		/// Because of a limitation of Android OrientationListener we always return faceup when being faceup or facedown.
		/// </summary>
		Faceup = 4,

		/// <summary>
		/// The device is face-down and the display is hidden from the user.
		/// </summary>
		Facedown = 5
	}
}
