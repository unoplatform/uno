#nullable enable

using System.ComponentModel;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	// CPanel::m_isIgnoringTransitions in WinUI. Only this panel writes it in Uno.
	internal bool IsIgnoringTransitions { get; set; }

	private void OnGroupStylePropertyChanged(object? sender, PropertyChangedEventArgs e)
		=> OnGroupStyleChanged(e.PropertyName);
}
