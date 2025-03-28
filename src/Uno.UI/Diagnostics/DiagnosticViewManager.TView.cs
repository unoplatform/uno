#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Windows.UI.Xaml;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A helper class used to create instances and propagate updates to a diagnostic view.
/// </summary>
/// <typeparam name="TView">Type of FrameworkElement used as diagnostic view.</typeparam>
/// <param name="factory">Factory to create an instance of the <typeparamref name="TView"/>.</param>
/// <param name="update">Delegate to use to update the <typeparamref name="TView"/> on <see cref="NotifyChanged"/>.</param>
internal class DiagnosticViewManager<TView>(Func<IDiagnosticViewContext, TView> factory, Action<IDiagnosticViewContext, TView> update)
	where TView : FrameworkElement
{
	private event EventHandler? _changed;

	public DiagnosticViewManager(Func<TView> factory, Action<TView> update)
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
