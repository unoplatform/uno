#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that can inject a JavaScript snippet
/// at the start of every document load (CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync).
/// </summary>
internal interface ISupportsDocumentCreatedScripts
{
	Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct);

	void RemoveScriptToExecuteOnDocumentCreated(string id);
}
