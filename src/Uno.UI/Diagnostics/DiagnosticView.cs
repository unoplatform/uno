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
internal partial class DiagnosticView(
	string name,
	Func<IDiagnosticViewContext, UIElement> preview,
	Func<IDiagnosticViewContext, CancellationToken, ValueTask<object?>>? details = null)
	: IDiagnosticViewProvider
{
	public DiagnosticView(
		string name,
		Func<UIElement> preview,
		Func<CancellationToken, ValueTask<object?>>? details = null)
		: this(name, _ => preview(), async (_, ct) => details is null ? null : await details(ct))
	{
	}

	/// <inheritdoc />
	string IDiagnosticViewProvider.Name => name;

	/// <inheritdoc />
	public object GetPreview(IDiagnosticViewContext context) 
		=> preview(context);

	/// <inheritdoc />
	public async ValueTask<object?> GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct) 
		=> details is null ? null : await details(context, ct);
}
