using System;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Foundation;
using System.Globalization;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.UI.ViewManagement.ApplicationView.NativeMethods;
#endif

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
#if !NET7_0_OR_GREATER
		private const string ApplicationViewTsType = "Windows.UI.ViewManagement.ApplicationView";
#endif

		public string Title
		{
			get
			{
#if NET7_0_OR_GREATER
				return NativeMethods.GetWindowTitle();
#else
				const string command = "Uno.UI.WindowManager.current.getWindowTitle()";
				return WebAssemblyRuntime.InvokeJS(command);
#endif
			}
			set
			{
#if NET7_0_OR_GREATER
				NativeMethods.SetWindowTitle(value);
#else
				var escapedValue = WebAssemblyRuntime.EscapeJs(value);
				var command = "Uno.UI.WindowManager.current.setWindowTitle(\"" + escapedValue + "\");";
				WebAssemblyRuntime.InvokeJS(command);
#endif
			}
		}

		public bool TryEnterFullScreenMode() => SetFullScreenMode(true);

		public void ExitFullScreenMode() => SetFullScreenMode(false);

		private bool SetFullScreenMode(bool turnOn)
		{
#if NET7_0_OR_GREATER
			return NativeMethods.SetFullScreenMode(turnOn);
#else
			var jsEval = $"{ApplicationViewTsType}.setFullScreenMode({turnOn.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()})";
			var result = WebAssemblyRuntime.InvokeJS(jsEval);
			return bool.TryParse(result, out var modeSwitchSuccessful) && modeSwitchSuccessful;
#endif
		}
	}
}
