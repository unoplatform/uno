using Uno.ApplicationModel.Core;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32CoreApplicationExtension : ICoreApplicationExtension
{
	public Win32CoreApplicationExtension()
	{
	}

	public bool CanExit => true;

	public void Exit()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Application has requested an exit");
		}

		Win32WindowWrapper.CloseAllWindows();
	}
}
