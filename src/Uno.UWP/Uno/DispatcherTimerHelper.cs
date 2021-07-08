#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno
{
	internal static class DispatcherTimerHelper
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
