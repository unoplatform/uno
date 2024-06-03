#nullable enable
using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Uno.Diagnostics.UI;

internal class DiagnosticViewHelper<TView, TState>(Func<IDiagnosticViewContext, TView> factory, Action<IDiagnosticViewContext, TView, TState> update)
	where TView : FrameworkElement
{
	private event EventHandler<TState>? _changed;
	private bool _hasState;
	private TState? _state;

	public DiagnosticViewHelper(Func<TView> factory, Action<TView, TState> update)
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
