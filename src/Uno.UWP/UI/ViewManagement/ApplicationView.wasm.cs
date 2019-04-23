using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
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
	}
}
