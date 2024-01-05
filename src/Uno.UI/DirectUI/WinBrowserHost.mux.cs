#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Core;

namespace Uno.UI.DirectUI;

internal class WinBrowserHost
{
	internal void PutSource(DependencyObject? dependencyObject)
	{
		CoreServices.Instance.PutVisualRoot(dependencyObject);
	}

	internal void PutEmptySource(bool firstLoad) => PutSource(null);
}
