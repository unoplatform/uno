using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Uno;
using Uno.Foundation;
using Uno.UI.Dispatching;

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
		internal static bool DispatchBackRequest()
		{
			// Invoked directly from the JS "popstate" handler, so it does not flow through
			// NativeDispatcher.RunAction where the UI-thread SynchronizationContext is normally
			// installed. Apply it here so BackRequested handlers observe a non-null
			// SynchronizationContext.Current (e.g. for async continuations that post back to the
			// UI thread). See https://github.com/unoplatform/uno/issues/23227.
			using (NativeDispatcher.Main.SynchronizationContext.Apply())
			{
				return GetForCurrentView().RequestBack();
			}
		}
	}
}
