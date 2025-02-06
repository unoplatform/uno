#nullable enable
#if WINUI || HAS_UNO_WINUI
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.Diagnostics.UI;

internal sealed class DiagnosticCounter(string name, string description) : IDiagnosticView
{
	private DiagnosticViewManager<TextBlock>? _preview;
	private long _value;

	public string Id => name;

	public string Name => name;

	public string Description => description;

	public long Value => _value;

	public void Increment()
	{
		Interlocked.Increment(ref _value);
		_preview?.NotifyChanged();
	}

	public void Decrement()
	{
		Interlocked.Decrement(ref _value);
		_preview?.NotifyChanged();
	}

	object IDiagnosticView.GetElement(IDiagnosticViewContext context)
		=> (_preview ??= DiagnosticViewHelper.CreateText(() => _value)).GetView(context);

	ValueTask<object?> IDiagnosticView.GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct)
		=> new($"current: {_value}\r\n\r\n{Description}");
}
#endif
