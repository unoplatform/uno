#nullable enable

using Uno.UI.DirectUI;
using Windows.UI.Core;

namespace DirectUI;

internal class JupiterControl
{
	internal static JupiterControl Create()
	{
		var control = new JupiterControl();
		//control.Init();
		return control;
	}

	private JupiterControl()
	{
	}

	internal void ConfigureJupiterWindow(CoreWindow? pCoreWindow)
	{
		// TODO Uno: This is not needed currently but we instead initialize the host here:
		WinBrowserHost.PutEmptySource(true);
	}
}
