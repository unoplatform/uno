#if __ANDROID__
#nullable enable
using System;
using Android.Content;
using Uno.Foundation.Logging;

namespace Uno.UI
{
	public static class ContextHelper
	{
		private static Android.Content.Context? _current;

		/// <summary>
		/// Gets the context of the most recently active activity that is still alive.
		/// </summary>
		/// <remarks>
		/// Driven by the activity lifecycle: an activity registers itself here when it is created,
		/// started or resumed, and deliberately stays registered while paused or stopped so work
		/// running in the background still resolves an activity. On teardown it is handed over to
		/// another live activity, or cleared when there is none, so a destroyed activity is not left
		/// as "current". This value is activity-scoped and may be <c>null</c> before any activity is
		/// created — app-scoped callers that only need a process context should use
		/// <see cref="ApplicationContext"/>.
		/// </remarks>
		public static Android.Content.Context? Current
		{
			get
			{
				if (_current is null && typeof(ContextHelper).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(ContextHelper)
						.Log()
						.Warn(
							"ContextHelper.Current not defined. " +
							"For compatibility with Uno, you should ensure your `MainActivity` " +
							"is deriving from Uno.UI.Runtime.Android.ApplicationActivity.");
				}

				return _current;
			}
			internal set => _current = value;
		}

		/// <summary>
		/// Gets the process-wide application context. Safe for app-scoped usage that does not
		/// depend on a specific window or foreground activity (system services, resources, package info).
		/// </summary>
		public static Android.Content.Context ApplicationContext => Android.App.Application.Context;

		/// <summary>
		/// Resolves the activity currently hosting a <c>Microsoft.UI.Xaml.Window</c>; provided by the runtime,
		/// which owns windows.
		/// </summary>
		internal static Func<object, Android.Content.Context?>? WindowContextResolver { get; set; }

		/// <summary>
		/// Tries getting the context of the most recently active live activity (see <see cref="Current"/>).
		/// </summary>
		/// <param name="context">The activity context if available.</param>
		/// <returns>true if a live activity context is available, otherwise false.</returns>
		internal static bool TryGetCurrent(out Android.Content.Context? context)
		{
			context = _current;
			return _current is not null;
		}

		/// <summary>
		/// Repoints <see cref="Current"/>, used by the activity lifecycle when the current
		/// activity is torn down. Passing <c>null</c> leaves <see cref="Current"/> without a
		/// live activity; app-scoped callers should use <see cref="ApplicationContext"/>.
		/// </summary>
		internal static void SetForeground(Android.Content.Context? context) => _current = context;
	}
}
#endif
