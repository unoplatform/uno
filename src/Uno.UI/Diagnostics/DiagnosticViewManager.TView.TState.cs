#nullable enable
using System;
using System.Linq;
using Windows.UI.Xaml;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A helper class used to create instances and propagate updates to a diagnostic view.
/// </summary>
/// <typeparam name="TView">Type of FrameworkElement used as diagnostic view.</typeparam>
/// <typeparam name="TState">Type of the state used to update the <typeparamref name="TView"/>.</typeparam>
/// <param name="factory">Factory to create an instance of the <typeparamref name="TView"/>.</param>
/// <param name="update">Delegate to use to update the <typeparamref name="TView"/> on <see cref="NotifyChanged"/>.</param>
internal class DiagnosticViewManager<TView, TState>(Func<IDiagnosticViewContext, TView> factory, Action<IDiagnosticViewContext, TView, TState> update)
	where TView : FrameworkElement
{
	private event EventHandler<TState>? _changed;
	private bool _hasState;
	private TState? _state;

	public DiagnosticViewManager(Func<TView> factory, Action<TView, TState> update)
		: this(_ => factory(), (_, view, state) => update(view, state))
	{
	}

	public void NotifyChanged(TState state)
	{
		_state = state;
		_hasState = true;
		_changed?.Invoke(this, state);
	}

	public UIElement GetView(IDiagnosticViewContext context)
	{
		var view = factory(context);
		EventHandler<TState> requestUpdate = (_, state) => context.Schedule(() => update(context, view, state));

		view.Loaded += (snd, e) =>
		{
			_changed += requestUpdate;
			if (_hasState)
			{
				requestUpdate(null, _state!);
			}
		};
		view.Unloaded += (snd, e) => _changed -= requestUpdate;

		return view;
	}
}
