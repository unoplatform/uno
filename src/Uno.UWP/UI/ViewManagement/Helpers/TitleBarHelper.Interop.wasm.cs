
namespace __Uno.UI.ViewManagement.Helpers
{
	internal partial class TitleBarHelper
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationViewTitleBar.setBackgroundColor")]
			internal static partial void SetBackgroundColor(string color);
		}
	}
}
