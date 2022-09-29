using System;

namespace Uno.Helpers
{
	internal static class DispatcherTimerProxy
	{
		private static Func<IDispatcherTimer>? _getter;

		/// <summary>
		/// Provides access to DispatcherTimer within the Uno.dll layer
		/// </summary>
		public static IDispatcherTimer? GetDispatcherTimer() => _getter?.Invoke();

		public static void SetDispatcherTimerGetter(Func<IDispatcherTimer> getter)
		{
			_getter = getter;
		}
	}
}
