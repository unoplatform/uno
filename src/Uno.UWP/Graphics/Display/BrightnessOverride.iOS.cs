using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using UIKit;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.Core;

namespace Windows.Graphics.Display
{
	public sealed partial class BrightnessOverride
	{
		private static UIScreen Window => UIScreen.MainScreen;

		/// <summary>
		/// Sets the brightness level within a range of 0 to 1 and the override options. 
		/// When your app is ready to change the current brightness with what you want to override it with, call StartOverride().
		/// </summary>
		/// <param name="brightnessLevel">double 0 to 1 </param>
		/// <param name="options"></param>
		public void SetBrightnessLevel(double brightnessLevel, DisplayBrightnessOverrideOptions options)
		{
			_targetBrightnessLevel = Math.Clamp(brightnessLevel, 0, 1);
		}

		/// <summary>
		/// Request to start overriding the screen brightness level.
		/// </summary>
		public void StartOverride()
		{
			this.Log().Debug("Starting brightness override");

			//This was added to make sure the brightness is restored when re-opening the app after a lock.
			CoreApplication.Resuming -= OnResuming;
			CoreApplication.Resuming += OnResuming;

			_defaultBrightnessLevel = Window.Brightness;

			Window.Brightness = (float)_targetBrightnessLevel;

			GetForCurrentView().IsOverrideActive = true;
		}

		/// <summary>
		/// Stops overriding the brightness level.
		/// </summary>
		public void StopOverride()
		{
			if (GetForCurrentView().IsOverrideActive)
			{
				this.Log().Debug("Stopping brightness override");

				CoreApplication.Resuming -= OnResuming;
				Window.Brightness = (float)_defaultBrightnessLevel;
				GetForCurrentView().IsOverrideActive = false;
			}
		}

		private void OnResuming(object sender, object e)
		{
			StartOverride();
		}
	}
}
