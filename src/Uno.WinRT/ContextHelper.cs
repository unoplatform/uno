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
		/// Gets or sets the context of the foreground activity.
		/// </summary>
		/// <remarks>
		/// The setter is driven by the activity lifecycle: the foreground activity registers
		/// itself here, and repoints on teardown (when another activity is alive) so a destroyed
		/// activity is not left as "current". This value is activity-scoped and may be
		/// <c>null</c> before any activity is created — app-scoped callers that only need a
		/// process context should use <see cref="ApplicationContext"/>, and callers that need a
		/// specific window's activity should resolve it from that window's <see cref="XamlRoot"/>
		/// rather than relying on this ambient value.
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
							"is deriving from Microsoft.UI.Xaml.ApplicationActivity.");
				}

				return _current;
			}
			set => _current = value;
		}

		/// <summary>
		/// Gets the process-wide application context. Safe for app-scoped usage that does not
		/// depend on a specific window or foreground activity (system services, resources, package info).
		/// </summary>
		public static Android.Content.Context ApplicationContext => Android.App.Application.Context;

		/// <summary>
		/// Tries getting the current foreground activity context.
		/// </summary>
		/// <param name="context">The foreground activity context if available.</param>
		/// <returns>true if a foreground activity context is available, otherwise false.</returns>
		internal static bool TryGetCurrent(out Android.Content.Context? context)
		{
			context = _current;
			return _current is not null;
		}

		/// <summary>
		/// Repoints the foreground context, used by the activity lifecycle when the current
		/// activity is torn down. Passing <c>null</c> leaves <see cref="Current"/> without a
		/// foreground activity; app-scoped callers should use <see cref="ApplicationContext"/>.
		/// </summary>
		internal static void SetForeground(Android.Content.Context? context) => _current = context;
	}
}
#endif
