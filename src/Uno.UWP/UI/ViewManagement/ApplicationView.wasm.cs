using Uno.Foundation;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		public string Title
		{
			get
			{
				const string command = "(function(){return document.title || \"\";})();";
				return WebAssemblyRuntime.InvokeJS(command);
			}
			set
			{
				var escapedValue = WebAssemblyRuntime.EscapeJs(value);
				var command = "(function(t){document.title = t; return \"\";})(\"" + escapedValue + "\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
		}
	}
}
