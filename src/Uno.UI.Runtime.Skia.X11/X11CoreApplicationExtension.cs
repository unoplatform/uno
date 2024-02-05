#nullable enable

using Uno.ApplicationModel.Core;
using Uno.Foundation.Logging;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11CoreApplicationExtension : ICoreApplicationExtension
{
	public X11CoreApplicationExtension()
	{
	}

	public bool CanExit => true;

	public void Exit()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Application has requested an exit");
		}

		X11XamlRootHost.CloseAllWindows();
	}
}
