using System;

namespace Uno.Helpers
{
	internal static class VisualTreeHelperProxy
	{
		private static Action? _closeAllFlyouts;

		public static void CloseAllFlyouts() => _closeAllFlyouts?.Invoke();

		public static void SetCloseAllFlyoutsAction(Action closeAllFlyouts) => _closeAllFlyouts = closeAllFlyouts;
	}
}
