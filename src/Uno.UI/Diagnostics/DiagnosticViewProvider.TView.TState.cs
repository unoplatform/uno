#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.Diagnostics.UI;

internal class DiagnosticView<TView, TState>(
	string name,
	Func<TView> preview,
	Action<TView, TState> update,
	Func<IDiagnosticViewContext, TState, CancellationToken, ValueTask<object?>>? details = null)
	: IDiagnosticViewProvider
	where TView : FrameworkElement
{
	private readonly DiagnosticViewHelper<TView, TState> _statusView = new(preview, update);

	private bool _hasState;
	private TState? _state;

	public void Update(TState status)
	{
		_state = status;
		_hasState = true;
		_statusView.NotifyChanged(status);
	}

	/// <inheritdoc />
	string IDiagnosticViewProvider.Name => name;

	/// <inheritdoc />
	object IDiagnosticViewProvider.GetPreview(IDiagnosticViewContext context)
		=> _statusView.GetView(context);

	/// <inheritdoc />
	async ValueTask<object?> IDiagnosticViewProvider.GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct)
		=> _hasState && details is not null ? await details(context, _state!, ct) : null;
}
