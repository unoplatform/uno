using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Windows.System.Display
{
    public partial class DisplayRequest
    {
		private const string JsType = "Windows.System.Display.DisplayRequest";

		partial void ActivateScreenLock()
		{
			var command = $"{JsType}.activateScreenLock()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		partial void DeactivateScreenLock()
		{
			var command = $"{JsType}.deactivateScreenLock()";
			WebAssemblyRuntime.InvokeJS(command);
		}
	}
}
