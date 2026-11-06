#nullable enable

using System.ComponentModel;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	private void OnGroupStylePropertyChanged(object? sender, PropertyChangedEventArgs e)
		=> OnGroupStyleChanged(e.PropertyName);
}
