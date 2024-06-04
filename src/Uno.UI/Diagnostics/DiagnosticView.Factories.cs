#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.Diagnostics.UI;

internal partial class DiagnosticView
{
	public static DiagnosticView<TView> Register<TView>(string name, Action<TView> update)
		where TView : FrameworkElement, new()
	{
		var provider = new DiagnosticView<TView>(name, () => new TView());
		DiagnosticViewRegistry.Register(provider);
		return provider;
	}

	public static DiagnosticView<TView, TState> Register<TView, TState>(
		string name, 
		Action<TView, TState> update,
		Func<TState, object?>? details = null)
		where TView : FrameworkElement, new()
	{
		var provider = details is null 
			? new DiagnosticView<TView, TState>(name, () => new TView(), update)
			: new DiagnosticView<TView, TState>(name, () => new TView(), update, (ctx, state, ct) => new(details(state));
		DiagnosticViewRegistry.Register(provider);
		return provider;
	}
}
