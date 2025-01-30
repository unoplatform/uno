using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.Popups
{
	internal partial class MessageDialog
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.alert")]
			internal static partial void Alert(string message);
		}
	}
}
