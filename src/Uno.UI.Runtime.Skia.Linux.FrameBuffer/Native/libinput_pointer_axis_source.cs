namespace Uno.UI.Runtime.Skia.Native
{ 
	enum libinput_pointer_axis_source
	{
		/**
		 * The event is caused by the rotation of a wheel.
		 */
		Wheel = 1,
		/**
		 * The event is caused by the movement of one or more fingers on a
		 * device.
		 */
		Finger,
		/**
		 * The event is caused by the motion of some device.
		 */
		Continuous,
		/**
		 * The event is caused by the tilting of a mouse wheel rather than
		 * its rotation. This method is commonly used on mice without
		 * separate horizontal scroll wheels.
		 *
		 * @deprecated This axis source is deprecated as of libinput 1.16.
		 * It was never used by any device before libinput 1.16. All wheel
		 * tilt devices use @ref LIBINPUT_POINTER_AXIS_SOURCE_WHEEL instead.
		 */
		WheelTilt,
	}
}
