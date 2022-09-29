#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Uno.Foundation;

namespace Windows.UI.Core
{
	partial class SystemNavigationManager
	{
		partial void OnAppViewBackButtonVisibility(AppViewBackButtonVisibility visibility)
		{
			switch (visibility)
			{
				case AppViewBackButtonVisibility.Visible:
					WebAssemblyRuntime.InvokeJS("Windows.UI.Core.SystemNavigationManager.current.enable();");
					break;

				case AppViewBackButtonVisibility.Collapsed:
				default: // Disabled value is not present in currently supported UWP API, but should be mapped to collapsed
					WebAssemblyRuntime.InvokeJS("Windows.UI.Core.SystemNavigationManager.current.disable();");
					break;
			}
		}

		[Preserve]
		public static bool DispatchBackRequest() => GetForCurrentView().RequestBack();
	}
}
