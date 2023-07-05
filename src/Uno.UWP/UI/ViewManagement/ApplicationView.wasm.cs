#nullable disable

using System;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Foundation;
using System.Globalization;

using NativeMethods = __Windows.UI.ViewManagement.ApplicationView.NativeMethods;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		public string Title
		{
			get
			{
				return NativeMethods.GetWindowTitle();
			}
			set
			{
				NativeMethods.SetWindowTitle(value);
			}
		}

		public bool TryEnterFullScreenMode() => SetFullScreenMode(true);

		public void ExitFullScreenMode() => SetFullScreenMode(false);

		private bool SetFullScreenMode(bool turnOn)
		{
			return NativeMethods.SetFullScreenMode(turnOn);
		}
	}
}
