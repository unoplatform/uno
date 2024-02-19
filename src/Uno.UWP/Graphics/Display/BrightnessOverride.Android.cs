using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Android.App;
using Uno.Extensions;

namespace Windows.Graphics.Display
{
	public sealed partial class BrightnessOverride
	{
		private static Android.Views.Window Window => (ContextHelper.Current as Activity)?.Window;

		private static Android.Views.WindowManagerLayoutParams LayoutParameters => Window?.Attributes;

		/// <summary>
		/// Sets the brightness level within a range of 0 to 1 and the override options.
		/// When your app is ready to change the current brightness with what you want to override it with, call StartOverride().
		/// </summary>
		/// <param name="brightnessLevel"> double 0 to 1  </param>
		/// <param name="options"></param>
		public void SetBrightnessLevel(double brightnessLevel, DisplayBrightnessOverrideOptions options)
		{
			if (_targetBrightnessLevel != brightnessLevel)
			{
				_defaultBrightnessLevel = LayoutParameters.ScreenBrightness;

				_targetBrightnessLevel = Math.Clamp(brightnessLevel, 0, 1);
			}
		}

		/// <summary>
		/// Request to start overriding the screen brightness level.
		/// </summary>
		public void StartOverride()
		{
			LayoutParameters.ScreenBrightness = (float)_targetBrightnessLevel;

			Window.Attributes = LayoutParameters;

			GetForCurrentView().IsOverrideActive = true;
		}

		/// <summary>
		/// Stops overriding the brightness level.
		/// </summary>
		public void StopOverride()
		{
			if (GetForCurrentView().IsOverrideActive)
			{
				LayoutParameters.ScreenBrightness = (float)_defaultBrightnessLevel;

				Window.Attributes = LayoutParameters;

				GetForCurrentView().IsOverrideActive = false;
			}
		}
	}
}
