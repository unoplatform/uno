using System.Collections.Generic;
using Microsoft.UI.Composition.Interactions;

namespace Uno.UI.Composition;

internal interface ICompositionTarget
{
	void TryRedirectForManipulation(Windows.UI.Input.PointerPoint pointerPoint, List<InteractionTracker> trackers);
}
