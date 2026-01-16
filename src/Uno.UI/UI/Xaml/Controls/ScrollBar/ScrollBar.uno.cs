using System;
using System.Linq;
using Uno.Disposables;
using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollBar
{
	private static void DetachEvents(object snd, RoutedEventArgs args) // OnUnloaded
		=> (snd as ScrollBar)?.DetachEvents();

#if !UNO_HAS_ENHANCED_LIFECYCLE
	private static void OnLayoutUpdated(
		object pSender,
		object pArgs)
	{
		(pSender as ScrollBar)?.UpdateTrackLayout();
	}
#endif
}
