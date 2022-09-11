using System;

namespace Windows.Graphics.Display
{
	/// <summary>
	/// Describes the options that modify the brightness level of the screen during the override session. When UseDimmedPolicyWhenBatteryIsLow is set, it reduces the specified override brightness level in order to conserve battery if the device battery is low during the override session. For example, if the override brightness level is set to 100% and UseDimmedPolicyWhenBatteryIsLow is set, the screen will dim to 70% instead.
	/// </summary>
	[Flags]
	public enum DisplayBrightnessOverrideOptions : uint
	{
		/// <summary>
		/// Screen display stays at the specified override brightness level when the device battery is low.
		/// </summary>
		None = 0U,
		/// <summary>
		/// Screen display dims when the device battery is low and a brightness override session is running.
		/// </summary>
		[Uno.NotImplemented]
		UseDimmedPolicyWhenBatteryIsLow = 1U
	}
}
