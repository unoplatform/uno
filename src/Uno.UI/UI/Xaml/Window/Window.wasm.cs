using System.Runtime.InteropServices.JavaScript;
using Uno;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

partial class Window
{
	[JSExport]
	[Preserve]
	internal static void Resize(double width, double height) => Uno.UI.Xaml.Controls.NativeWindowWrapper.Instance.RaiseNativeSizeChanged(width, height);
}
