namespace Uno.UI.RemoteControl.Host.HotReload;

/// <summary>
/// Who reported a hot-reload event: the IDE, through the IDE channel, or the dev-server itself.
/// </summary>
internal enum HotReloadEventSource
{
	IDE,
	DevServer
}
