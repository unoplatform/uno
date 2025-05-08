#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.UI.Extensions;

namespace Uno.UI.RuntimeTests.Extensions;

internal static class UIElementExtensions
{
	public static List<string> SubscribeToPointerEvents(this UIElement elt, List<string>? pointerEvents = null, [CallerArgumentExpression(nameof(elt))] string? name = null)
	{
		pointerEvents ??= new();
		if (elt is FrameworkElement { Name: { Length: > 0 } fwEltName })
		{
			name = fwEltName;
		}
		name ??= elt.GetDebugName();

		// Raw pointers
		elt.PointerEntered += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.PointerEntered));
		elt.PointerExited += (snd, e) => pointerEvents.Add(name + "." + name + "." + nameof(UIElement.PointerExited));
		elt.PointerPressed += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.PointerPressed));
		elt.PointerReleased += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.PointerReleased));
		elt.PointerMoved += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.PointerMoved));
		elt.PointerCaptureLost += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.PointerCaptureLost));
		elt.PointerCanceled += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.PointerCanceled));
		elt.PointerWheelChanged += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.PointerWheelChanged));

		// Gestures
		elt.Tapped += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.Tapped));
		elt.DoubleTapped += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.DoubleTapped));
		elt.RightTapped += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.RightTapped));
		elt.Holding += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.Holding));

		// Manipulation
		elt.ManipulationStarting += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.ManipulationStarting));
		elt.ManipulationStarted += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.ManipulationStarted));
		elt.ManipulationDelta += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.ManipulationDelta));
		elt.ManipulationInertiaStarting += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.ManipulationInertiaStarting));
		elt.ManipulationCompleted += (snd, e) => pointerEvents.Add(name + "." + nameof(UIElement.ManipulationCompleted));

		return pointerEvents;
	}
}
