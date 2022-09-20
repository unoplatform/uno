#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Provider;
using Windows.Extensions;
using Windows.Foundation;

namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Provides API to interact with the user’s Appointments provider app (for example, the Calendar app).
/// Call static methods to display provider-specific UI so that the user can perform tasks.
/// </summary>
public partial class AppointmentManager
{
	/// <summary>
	/// Shows the Appointments provider app's primary UI.
	/// This typically displays a time frame from an appointments calendar.
	/// </summary>
	/// <remarks>
	/// The duration parameter is ignored as it is not supported on Android.
	/// </remarks>
	/// <param name="timeToShow">
	/// A date and time object that specifies the beginning
	/// of the time frame that the Appointments provider
	/// app should display.
	/// </param>
	/// <param name="duration">
	/// A timespan that hints to the Appointments provider
	/// app how long the time frame shown should be.
	/// </param>
	/// <returns>Completes asynchonously.</returns>
	public static IAsyncAction ShowTimeFrameAsync(DateTimeOffset timeToShow, TimeSpan duration)
		=> ShowTimeFrameAsyncTask(timeToShow, duration).AsAsyncAction();

	/// <summary>
	/// Requests the AppointmentStore object associated with the calling application.
	/// </summary>
	/// <param name="options">
	/// An AppointmentStoreAccessType value indicating the level
	/// of access the returned appointment store will have.
	/// </param>
	/// <returns>Appointment store.</returns>
	public static IAsyncOperation<AppointmentStore?> RequestStoreAsync(AppointmentStoreAccessType options)
		=> RequestStoreAsyncTask(options).AsAsyncOperation();

	private static async Task ShowTimeFrameAsyncTask(DateTimeOffset timeToShow, TimeSpan duration)
	{
		var builder = CalendarContract.ContentUri?.BuildUpon();
		if (builder is null)
		{
			throw new InvalidOperationException("Calendar content URI builder is null");
		}

		builder.AppendPath("time");
		ContentUris.AppendId(builder, timeToShow.ToUniversalTime().ToUnixTimeMilliseconds());
		var intent = new Intent(Intent.ActionView).SetData(builder.Build());
		Application.Context.StartActivity(intent);
	}

	private static async Task<AppointmentStore?> RequestStoreAsyncTask(AppointmentStoreAccessType options)
	{
		if (options != AppointmentStoreAccessType.AllCalendarsReadOnly)
		{
			throw new NotSupportedException("Only AllCalendarsReadOnly is implemented");
		}

		var requiredPermission = Android.Manifest.Permission.ReadCalendar;

		if (!PermissionsHelper.IsDeclaredInManifest(requiredPermission))
		{
			throw new UnauthorizedAccessException("ReadCalendar permission is not declared in Manifest");
		}

		var permissionGranted = await PermissionsHelper.CheckPermission(CancellationToken.None, requiredPermission);

		if (!permissionGranted &&
			!await PermissionsHelper.TryGetPermission(CancellationToken.None, requiredPermission))
		{
			return null;
		}

		return new();
	}
}
