#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A diagnostic entry that can be displayed by the DiagnosticsOverlay.
/// </summary>
public interface IDiagnosticViewProvider
{
	/// <summary>
	/// Friendly name of the diagnostic.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Get a preview of the diagnostic, usually a value or an icon.
	/// </summary>
	/// <param name="context">An update coordinator that can be used to push updates on the preview.</param>
	/// <returns>Either a UIElement to be displayed by the diagnostic overlay or a plain object (rendered as text).</returns>
	/// <remarks>This is expected to be invoked on the dispatcher used to render the preview.</remarks>
	object GetPreview(IDiagnosticViewContext context);

	/// <summary>
	/// Show details of the diagnostic.
	/// </summary>
	/// <param name="context">An update coordinator that can be used to push updates on the preview.</param>
	/// <param name="ct">Token to cancel the async operation.</param>
	/// <returns>Either a UIElement to be displayed by the diagnostic overlay, a ContentDialog to show, or a simple object to show in a content dialog.</returns>
	/// <remarks>This is expected to be invoked on the dispatcher used to render the preview.</remarks>
	ValueTask<object?> GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct);
}
