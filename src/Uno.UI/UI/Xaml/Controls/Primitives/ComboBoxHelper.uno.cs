#nullable enable

using System;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ComboBoxHelper
{
	private static IDisposable CreateDropDownEventRevokers(ComboBox comboBox)
	{
		comboBox.DropDownOpened += OnDropDownOpened;
		comboBox.DropDownClosed += OnDropDownClosed;

		var weakComboBox = new WeakReference<ComboBox>(comboBox);
		return Disposable.Create(() =>
		{
			if (weakComboBox.TryGetTarget(out var target))
			{
				target.DropDownOpened -= OnDropDownOpened;
				target.DropDownClosed -= OnDropDownClosed;
			}
		});
	}

	private static void SetDropDownEventRevokers(ComboBox comboBox, IDisposable? revokers)
	{
		// WinRT replacement releases the old revoker immediately; CLR collection does not call Dispose.
		(comboBox.GetValue(DropDownEventRevokersProperty) as IDisposable)?.Dispose();
		comboBox.SetValue(DropDownEventRevokersProperty, revokers);
	}
}
