#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Core;

namespace Uno.UI.DirectUI;

internal static class WinBrowserHost
{
	private static void PutSource(DependencyObject? dependencyObject)
	{
		CoreServices.Instance.PutVisualRoot(dependencyObject);
	}

	internal static void PutEmptySource(bool firstLoad) => PutSource(null);
}
