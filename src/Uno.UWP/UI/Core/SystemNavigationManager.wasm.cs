using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Uno;
using Uno.Foundation;

using NativeMethods = __Windows.UI.Core.SystemNavigationManager.NativeMethods;

namespace Windows.UI.Core
{
	partial class SystemNavigationManager
	{
		partial void OnAppViewBackButtonVisibility(AppViewBackButtonVisibility visibility)
		{
			switch (visibility)
			{
				case AppViewBackButtonVisibility.Visible:
					NativeMethods.Enable();
					break;

				case AppViewBackButtonVisibility.Collapsed:
				default: // Disabled value is not present in currently supported UWP API, but should be mapped to collapsed
					NativeMethods.Disable();
					break;
			}
		}

		[Preserve]
		[JSExport]
		internal static bool DispatchBackRequest() => GetForCurrentView().RequestBack();
	}
}
