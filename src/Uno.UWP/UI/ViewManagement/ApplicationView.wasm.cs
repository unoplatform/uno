using Uno.Foundation;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
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
