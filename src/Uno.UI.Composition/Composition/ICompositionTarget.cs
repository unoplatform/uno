using System.Collections.Generic;
using Windows.UI.Composition.Interactions;

namespace Uno.UI.Composition;

internal interface ICompositionTarget
{
	void TryRedirectForManipulation(Windows.UI.Input.PointerPoint pointerPoint, InteractionTracker tracker);
}
