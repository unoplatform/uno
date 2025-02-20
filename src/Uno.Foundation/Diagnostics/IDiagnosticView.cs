#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A diagnostic entry that can be displayed by the DiagnosticsOverlay.
/// </summary>
public interface IDiagnosticView
{
	/// <summary>
	/// Identifier of the diagnostic view than can be used to request to show or remove it from a DiagnosticsOverlay.
	/// </summary>
	string Id { get; }

	/// <summary>
	/// Friendly name of the diagnostic.
	/// </summary>
	string Name { get; }

	DiagnosticViewRegistrationPosition Position => DiagnosticViewRegistrationPosition.Normal;

	/// <summary>
	/// Gets a visual element of the diagnostic, usually a value or an icon.
	/// </summary>
	/// <param name="context">An update coordinator that can be used to push updates on the preview.</param>
	/// <returns>Either a UIElement to be displayed by the diagnostic overlay or a plain object (rendered as text).</returns>
	/// <remarks>This is expected to be invoked on the dispatcher used to render the preview.</remarks>
	object GetElement(IDiagnosticViewContext context);

	/// <summary>
	/// Show details of the diagnostic.
	/// </summary>
	/// <param name="context">An update coordinator that can be used to push updates on the preview.</param>
	/// <param name="ct">Token to cancel the async operation.</param>
	/// <returns>Either a UIElement to be displayed by the diagnostic overlay, a ContentDialog to show, or a simple object to show in a content dialog.</returns>
	/// <remarks>This is expected to be invoked on the dispatcher used to render the preview.</remarks>
	ValueTask<object?> GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct);
}
