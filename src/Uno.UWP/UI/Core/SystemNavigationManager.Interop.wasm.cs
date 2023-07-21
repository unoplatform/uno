using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.Core
{
	internal partial class SystemNavigationManager
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.UI.Core.SystemNavigationManager.current.disable")]
			internal static partial void Disable();

			[JSImport("globalThis.Windows.UI.Core.SystemNavigationManager.current.enable")]
			internal static partial void Enable();
		}
	}
}
