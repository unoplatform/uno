#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A generic diagnostic view.
/// </summary>
internal class DiagnosticView<TView>(
	string id,
	string name,
	Func<IDiagnosticViewContext, TView> factory,
	Func<IDiagnosticViewContext, CancellationToken, ValueTask<object?>>? details = null)
	: IDiagnosticView
	where TView : UIElement
{
	public DiagnosticView(
		string id,
		string name,
		Func<TView> preview,
		Func<CancellationToken, ValueTask<object?>>? details = null)
		: this(id, name, _ => preview(), async (_, ct) => details is null ? null : await details(ct))
	{
	}

	/// <inheritdoc />
	string IDiagnosticView.Id => id;

	/// <inheritdoc />
	string IDiagnosticView.Name => name;

	/// <inheritdoc />
	object IDiagnosticView.GetElement(IDiagnosticViewContext context) => factory(context);

	/// <inheritdoc />
	async ValueTask<object?> IDiagnosticView.GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct)
		=> details is null ? null : await details(context, ct);
}
