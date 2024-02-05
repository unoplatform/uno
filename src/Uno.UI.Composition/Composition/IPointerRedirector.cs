#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Composition.Interactions;
using Windows.UI.Input;

namespace Microsoft.UI.Composition;

internal interface IPointerRedirector
{
	void RedirectPointer(PointerPoint pointer, List<InteractionTracker> trackers);
}
