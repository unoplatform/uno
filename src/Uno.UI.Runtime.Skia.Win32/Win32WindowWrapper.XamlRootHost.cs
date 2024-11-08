#nullable enable

using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : IXamlRootHost
{
	public UIElement? RootElement => Window?.RootElement;

	public unsafe void InvalidateRender()
	{
		_ = PInvoke.InvalidateRect(_hwnd, default(RECT*), true)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.InvalidateRect)} failed: {Win32Helper.GetErrorMessage()}")
	}
}
