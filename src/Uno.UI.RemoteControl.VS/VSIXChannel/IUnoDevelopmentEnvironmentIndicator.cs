using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.IDE;

public interface IUnoDevelopmentEnvironmentIndicator
{
	/*
	* WARNING WARNING WARNING WARNING WARNING WARNING WARNING
	*
	* This interface is shared between Uno's VS extension and the Uno.RC package, make sure to keep in sync.
	* In order to avoid versioning issues, avoid modifications and **DO NOT** remove any member from this interface.
	*
	*/

	ValueTask NotifyAsync(DevelopmentEnvironmentStatusIdeMessage message, CancellationToken ct); // For backward compat with VS.RC, we keep this directly on the interface instead of moving it to extension method!

	ValueTask NotifyAsync(DevelopmentEnvironmentStatusIdeMessage[] messages, CancellationToken ct);

	/// <summary>
	/// Request to cleanup the indicator (e.g. on solution close).
	/// This will remove all components from the indicator and show only the provided messages.
	/// </summary>
	ValueTask CleanupAsync(DevelopmentEnvironmentStatusIdeMessage[] messages, CancellationToken ct);
}
