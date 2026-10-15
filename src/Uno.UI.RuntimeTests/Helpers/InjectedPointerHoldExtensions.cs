#nullable enable

using System;
using System.Threading.Tasks;
using Uno.UI.DevTools.Input;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Helpers;

internal static class InjectedPointerHoldExtensions
{
	/// <summary>
	/// Presses and releases at the same location once WinUI's 800 ms hold delay has elapsed: a touch or pen hold.
	/// </summary>
	/// <param name="advanceTimestamps">
	/// Whether the pointer timestamps also advance by the hold duration, like a device's. On Uno, injected timestamps
	/// only advance with the injected time offsets, so otherwise only the holding timer sees the hold.
	/// </param>
	public static async Task Hold(this IInjectedPointer pointer, Point location, bool advanceTimestamps = true)
	{
		pointer.Press(location);
		await Task.Delay(TimeSpan.FromSeconds(1));
#if HAS_UNO
		if (advanceTimestamps)
		{
			pointer.MoveTo(location, steps: 0, stepOffsetInMilliseconds: 1000);
		}
#endif
		pointer.Release();
	}
}
