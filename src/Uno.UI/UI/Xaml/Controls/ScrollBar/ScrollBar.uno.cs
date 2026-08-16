using System;
using System.Linq;
using Uno.Disposables;
using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollBar
{
	private static void DetachEvents(object snd, RoutedEventArgs args) // OnUnloaded
		=> (snd as ScrollBar)?.DetachEvents();

}
