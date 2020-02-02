using System;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Windows.Foundation;
using System.Globalization;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		private const string ApplicationViewTsType = "Windows.UI.ViewManagement.ApplicationView";

		internal void SetVisibleBounds(Rect newVisibleBounds)
		{
			if (newVisibleBounds != VisibleBounds)
			{
				VisibleBounds = newVisibleBounds;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Updated visible bounds {VisibleBounds}");
				}

				VisibleBoundsChanged?.Invoke(this, null);
			}
		}

		public string Title
		{
			get
			{
				const string command = "Uno.UI.WindowManager.current.getWindowTitle()";
				return WebAssemblyRuntime.InvokeJS(command);
			}
			set
			{
				var escapedValue = WebAssemblyRuntime.EscapeJs(value);
				var command = "Uno.UI.WindowManager.current.setWindowTitle(\"" + escapedValue + "\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
		}

		public bool TryEnterFullScreenMode() => SetFullScreenMode(true);

		public void ExitFullScreenMode() => SetFullScreenMode(false);

		private bool SetFullScreenMode(bool turnOn)
		{
			var jsEval = $"{ApplicationViewTsType}.setFullScreenMode({turnOn.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()})";
			var result = WebAssemblyRuntime.InvokeJS(jsEval);
			return bool.TryParse(result, out var modeSwitchSuccessful) && modeSwitchSuccessful;			
		}
	}
}
