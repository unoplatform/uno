using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

namespace Windows.Graphics.Display
{
	public sealed partial class BrightnessOverride
	{
		/// <summary>
		/// Sets the brightness level within a range of 0 to 1 and the override options. 
		/// When your app is ready to change the current brightness with what you want to override it with, call StartOverride().
		/// </summary>
		/// <param name="brightnessLevel">double 0 to 1 </param>
		/// <param name="options"></param>
		public void SetBrightnessLevel(double brightnessLevel, DisplayBrightnessOverrideOptions options)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Request to start overriding the screen brightness level.
		/// </summary>
		public void StartOverride()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stops overriding the brightness level.
		/// </summary>
		public void StopOverride()
		{
			throw new NotSupportedException();
		}
	}
}
