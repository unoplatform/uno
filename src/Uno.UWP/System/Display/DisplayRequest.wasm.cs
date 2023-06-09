using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.__System.Display.DisplayRequest.NativeMethods;
#endif

namespace Windows.System.Display
{
	public partial class DisplayRequest
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.System.Display.DisplayRequest";
#endif

		partial void ActivateScreenLock()
		{
#if NET7_0_OR_GREATER
			NativeMethods.ActivateScreenLock();
#else
			var command = $"{JsType}.activateScreenLock()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		partial void DeactivateScreenLock()
		{
#if NET7_0_OR_GREATER
			NativeMethods.DeactivateScreenLock();
#else
			var command = $"{JsType}.deactivateScreenLock()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}
	}
}
