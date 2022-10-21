#nullable enable

using Gtk;
using Uno.ApplicationModel.Core;

namespace Uno.Extensions.ApplicationModel.Core;

internal class CoreApplicationExtension : ICoreApplicationExtension
{
	public CoreApplicationExtension(object? owner)
	{			
	}
	
	public bool CanExit => true;

	public void Exit() => Application.Quit();
}
