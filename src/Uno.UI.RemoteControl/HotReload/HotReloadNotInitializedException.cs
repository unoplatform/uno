#nullable enable

using System;

namespace Uno.UI.RemoteControl.HotReload;

/// <summary>
/// Reported when a file-update request is issued before the hot-reload engine has published its
/// first status notification, i.e. before the first
/// <see cref="ClientHotReloadProcessor.StatusChanged"/> event. This is a transient startup
/// condition: wait for that notification, then retry the request.
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/> so existing handlers keep working, while
/// letting tooling distinguish this transient "not initialized yet" rejection from the terminal
/// update failures that also surface as <see cref="InvalidOperationException"/>.
/// </remarks>
public sealed class HotReloadNotInitializedException : InvalidOperationException
{
	internal HotReloadNotInitializedException()
		: base(
			"Hot reload is not initialized yet (no status has been received from the hot-reload engine). "
			+ "Wait for the first ClientHotReloadProcessor.StatusChanged notification before sending file updates.")
	{
	}
}
