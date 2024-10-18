#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A generic diagnostic view that can be updated with a state.
/// </summary>
internal class DiagnosticView<TView, TState>(
	string id,
	string name,
	Func<IDiagnosticViewContext, TView> factory,
	Action<TView, TState> update,
	Func<IDiagnosticViewContext, TState, CancellationToken, ValueTask<object?>>? details = null)
	: IDiagnosticView
	where TView : FrameworkElement
{
	private readonly DiagnosticViewManager<TView, TState> _elementsManager = new(factory, (_, view, state) => update(view, state));

	private bool _hasState;
	private TState? _state;

	public void Update(TState status)
	{
		_state = status;
		_hasState = true;
		_elementsManager.NotifyChanged(status);
	}

	/// <inheritdoc />
	string IDiagnosticView.Id => id;

	/// <inheritdoc />
	string IDiagnosticView.Name => name;

	/// <inheritdoc />
	object IDiagnosticView.GetElement(IDiagnosticViewContext context)
		=> _elementsManager.GetView(context);

	/// <inheritdoc />
	async ValueTask<object?> IDiagnosticView.GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct)
		=> _hasState && details is not null ? await details(context, _state!, ct) : null;
}
