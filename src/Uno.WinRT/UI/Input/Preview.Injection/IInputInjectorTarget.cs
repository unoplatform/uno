#nullable enable
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

internal interface IInputInjectorTarget
{
	void InjectPointerAdded(PointerEventArgs args);

	void InjectPointerUpdated(PointerEventArgs args);

	void InjectPointerRemoved(PointerEventArgs args);

	/// <summary>
	/// Gets the bounds, in the same coordinate space as injected <see cref="PointerEventArgs"/> positions,
	/// used to resolve <see cref="InjectedInputMouseOptions.Absolute"/> normalized coordinates.
	/// </summary>
	Size GetInjectionBounds();
}
