#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Helpers;

internal static class InjectedPointerHoldExtensions
{
	/// <summary>
	/// Presses and releases at the same location as a touch or pen hold.
	/// </summary>
	/// <param name="timerOnlyHoldingSource">
	/// On Uno, an element which listens to Holding: the hold then lasts until it raises Holding and the pointer timestamps don't advance,
	/// so only its holding timer sees the hold. Otherwise the timestamps advance past the hold delay, like a device's.
	/// </param>
	public static async Task Hold(this IInjectedPointer pointer, Point location, UIElement? timerOnlyHoldingSource = null)
	{
		pointer.Press(location);
#if HAS_UNO
		if (timerOnlyHoldingSource is null)
		{
			pointer.MoveTo(location, steps: 0, stepOffsetInMilliseconds: 1000);
		}
		else
		{
			var isHolding = false;
			HoldingEventHandler onHolding = (_, e) => isHolding |= e.HoldingState is Microsoft.UI.Input.HoldingState.Started;
			timerOnlyHoldingSource.Holding += onHolding;
			try
			{
				await UITestHelper.WaitFor(() => isHolding, timeoutMS: 5000, message: $"{timerOnlyHoldingSource} did not raise Holding.");
			}
			finally
			{
				timerOnlyHoldingSource.Holding -= onHolding;
			}
		}
#else
		// Windows only recognizes an injected hold when the contact keeps reporting frames.
		for (var i = 0; i < 10; i++)
		{
			await Task.Delay(100);
			pointer.MoveTo(location, steps: 0, stepOffsetInMilliseconds: 0);
		}
#endif
		pointer.Release();
	}
}
