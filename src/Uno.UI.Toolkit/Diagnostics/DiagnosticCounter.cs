#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.Diagnostics.UI;

internal sealed class DiagnosticCounter(string name, string description) : IDiagnosticViewProvider
{
	private DiagnosticViewHelper<TextBlock>? _preview;
	private long _value;

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

	object IDiagnosticViewProvider.GetPreview(IDiagnosticViewContext context)
		=> (_preview ??= DiagnosticViewHelper.CreateText(() => _value)).GetView(context);

	ValueTask<object?> IDiagnosticViewProvider.GetDetailsAsync(IDiagnosticViewContext context, CancellationToken ct)
		=> new($"current: {_value}\r\n\r\n{Description}");
}
