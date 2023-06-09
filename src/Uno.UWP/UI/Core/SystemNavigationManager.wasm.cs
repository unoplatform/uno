using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Uno.Foundation;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

using NativeMethods = __Windows.UI.Core.SystemNavigationManager.NativeMethods;
#endif

namespace Windows.UI.Core
{
	partial class SystemNavigationManager
	{
		partial void OnAppViewBackButtonVisibility(AppViewBackButtonVisibility visibility)
		{
			switch (visibility)
			{
				case AppViewBackButtonVisibility.Visible:
#if NET7_0_OR_GREATER
					NativeMethods.Enable();
#else
					WebAssemblyRuntime.InvokeJS("Windows.UI.Core.SystemNavigationManager.current.enable();");
#endif
					break;

				case AppViewBackButtonVisibility.Collapsed:
				default: // Disabled value is not present in currently supported UWP API, but should be mapped to collapsed
#if NET7_0_OR_GREATER
					NativeMethods.Disable();
#else
					WebAssemblyRuntime.InvokeJS("Windows.UI.Core.SystemNavigationManager.current.disable();");
#endif
					break;
			}
		}

		[Preserve]
#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static bool DispatchBackRequest() => GetForCurrentView().RequestBack();
	}
}
