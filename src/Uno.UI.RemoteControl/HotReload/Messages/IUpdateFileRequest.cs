using System;

namespace Uno.UI.RemoteControl.HotReload.Messages;

public interface IUpdateFileRequest
{
	/// <summary>
	/// ID of this file update request.
	/// </summary>
	string RequestId { get; }

	/// <summary>
	/// If true, the file will be saved on disk, even if the content is the same.
	/// NULL means the default behavior will be used according to the capabilities of the IDE (our integration).
	/// </summary>
	/// <remarks>
	/// Currently, this is only used for VisualStudio, because the update requires a file save on disk for other IDEs.
	/// On VisualStudio, the save to disk is not required for doing Hot Reload.
	/// </remarks>
	bool? ForceSaveOnDisk { get; }

	/// <summary>
	/// Disable the forced hot-reload requested on VS after the file has been modified.
	/// </summary>
	bool IsForceHotReloadDisabled { get; }

	/// <summary>
	/// The delay to wait before forcing (**OR RETRYING**) a hot reload in Visual Studio.
	/// </summary>
	TimeSpan? ForceHotReloadDelay { get; }

	/// <summary>
	/// Number of times to retry the hot reload in Visual Studio **if not changes are detected**.
	/// </summary>
	int? ForceHotReloadAttempts { get; }
}
