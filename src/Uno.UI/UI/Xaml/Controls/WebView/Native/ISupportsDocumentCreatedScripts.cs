#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebViews that can run a script when each new document is created, before its own scripts.
/// </summary>
internal interface ISupportsDocumentCreatedScripts
{
	/// <remarks>
	/// The registration must be issued synchronously, so that a navigation started right after the call already runs the script.
	/// </remarks>
	Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct);

	void RemoveScriptToExecuteOnDocumentCreated(string id);
}
