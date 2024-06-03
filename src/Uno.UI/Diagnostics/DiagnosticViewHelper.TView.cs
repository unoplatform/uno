#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Uno.Diagnostics.UI;

internal class DiagnosticViewHelper<TView>(Func<IDiagnosticViewContext, TView> factory, Action<IDiagnosticViewContext, TView> update)
	where TView : FrameworkElement
{
	private event EventHandler? _changed;

	public DiagnosticViewHelper(Func<TView> factory, Action<TView> update)
		: this(_ => factory(), (_, t) => update(t))
	{
	}

	public void NotifyChanged()
	{
		_changed?.Invoke(this, EventArgs.Empty);
	}

	public UIElement GetView(IDiagnosticViewContext context)
	{
		var view = factory(context);
		EventHandler requestUpdate = (_, __) => context.Schedule(() => update(context, view));

		view.Loaded += (snd, e) =>
		{
			_changed += requestUpdate;
			requestUpdate(null, EventArgs.Empty);
		};
		view.Unloaded += (snd, e) => _changed -= requestUpdate;

		return view;
	}
}
