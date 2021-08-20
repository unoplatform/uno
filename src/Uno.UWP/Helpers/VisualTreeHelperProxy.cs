#nullable enable

using System;

namespace Uno.Helpers
{
	internal static class VisualTreeHelperProxy
	{
		private static Action? _closeAllPopups;

		public static void CloseAllPopups() => _closeAllPopups?.Invoke();

		public static void SetCloseAllPopupsAction(Action closeAllPopups) => _closeAllPopups = closeAllPopups;
	}
}
