#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A generic diagnostic view provider.
/// </summary>
internal class DiagnosticView<TView>(
	string name,
	Func<IDiagnosticViewContext, TView> preview,
	Func<IDiagnosticViewContext, CancellationToken, ValueTask<object?>>? details = null)
	: IDiagnosticViewProvider
	where TView : UIElement
{
	public DiagnosticView(
		string name,
		Func<TView> preview,
		Func<CancellationToken, ValueTask<object?>>? details = null)
		: this(name, _ => preview(), async (_, ct) => details is null ? null : await details(ct))
	{
	}

	/// <inheritdoc />
	string IDiagnosticViewProvider.Name => name;

	/// <inheritdoc />
	object IDiagnosticViewProvider.GetPreview(IDiagnosticViewContext context) => preview(context);

	/// <inheritdoc />
	async ValueTask<object?> IDiagnosticViewProvider.GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct)
		=> details is null ? null : await details(context, ct);
}
