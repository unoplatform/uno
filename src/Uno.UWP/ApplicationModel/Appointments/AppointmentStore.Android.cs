#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using AndroidEventColumns = Android.Provider.CalendarContract.IEventsColumns;

namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Represents a store that contains appointments.
/// </summary>
public partial class AppointmentStore
{
	private const int DefaultReminderInMinutes = 15;

	private static readonly IReadOnlyDictionary<string, string> _appointmentPropertyMap =
		new Dictionary<string, string>()
		{
			{AppointmentProperties.AllDay, AndroidEventColumns.AllDay},
			{AppointmentProperties.Location, AndroidEventColumns.EventLocation},
			{AppointmentProperties.Subject, AndroidEventColumns.Title},
			{AppointmentProperties.Organizer, AndroidEventColumns.Organizer},
			{AppointmentProperties.Details, AndroidEventColumns.Description},
			{AppointmentProperties.Reminder, AndroidEventColumns.HasAlarm}
		};

	private bool _startTimeRequested;
	private bool _durationRequested;

	public IAsyncOperation<IReadOnlyList<Appointment>?> FindAppointmentsAsync(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options) =>
		FindAppointmentsAsyncTask(rangeStart, rangeLength, options).AsAsyncOperation();

	private async Task<IReadOnlyList<Appointment>?> FindAppointmentsAsyncTask(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
	{
		List<Appointment> entriesList = new();

		if (options is null)
		{
			return null;
		}

		var builder = Android.Provider.CalendarContract.Instances.ContentUri?.BuildUpon();
		if (builder == null)
		{
			throw new NullReferenceException("Windows.ApplicationModel.Appointments.AppointmentStore.FindAppointmentsAsyncTask, builder is null (impossible)");
		}
		Android.Content.ContentUris.AppendId(builder, rangeStart.ToUniversalTime().ToUnixTimeMilliseconds());
		var rangeEnd = rangeStart + rangeLength;
		Android.Content.ContentUris.AppendId(builder, rangeEnd.ToUniversalTime().ToUnixTimeMilliseconds());
		var uri = builder.Build();
		// it is simply: {content://com.android.calendar/instances/when/1588275364371/1588880164371}
		if (uri == null)
		{
			throw new NullReferenceException("Windows.ApplicationModel.Appointments.AppointmentStore.FindAppointmentsAsyncTask, oUri is null (impossible)");
		}

		var androidColumns = ConvertWinRTToAndroidColumnNames(options.FetchProperties);
		// some 'system columns' columns, cannot be switched off
		androidColumns.Add(AndroidEventColumns.CalendarId);
		androidColumns.Add("_id");
		androidColumns.Add(Android.Provider.CalendarContract.Instances.Begin);    // for sort
		androidColumns.Add(Android.Provider.CalendarContract.Instances.End);    // we need this, as Android sometimes has NULL duration, and it should be reconstructed from start/end

		// string sortMode = Android.Provider.CalendarContract.EventsColumns.Dtstart + " ASC";
		var sortMode = Android.Provider.CalendarContract.Instances.Begin + " ASC";
		var contentResolver = Android.App.Application.Context.ContentResolver;
		if (contentResolver == null)
		{
			throw new NullReferenceException("Windows.ApplicationModel.Appointments.AppointmentStore.FindAppointmentsAsyncTask, _contentResolver is null (impossible)");
		}

		using var cursor = contentResolver?.Query(uri,
								androidColumns.ToArray(),  // columns in result
								null,   // where
								null,   // where params
								sortMode);

		if (cursor is null || cursor.IsAfterLast)
		{
			return entriesList;
		}

		if (!cursor.MoveToFirst())
		{
			return entriesList;
		}

		// optimization
		int colAllDay = cursor.GetColumnIndex(AndroidEventColumns.AllDay);
		int colLocation = cursor.GetColumnIndex(AndroidEventColumns.EventLocation);
		int colStartTime = cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.Begin);
		int colEndTime = cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.End);
		int colSubject = cursor.GetColumnIndex(AndroidEventColumns.Title);
		int colOrganizer = cursor.GetColumnIndex(AndroidEventColumns.Organizer);
		int colDetails = cursor.GetColumnIndex(AndroidEventColumns.Description);
		int colCalId = cursor.GetColumnIndex(AndroidEventColumns.CalendarId);
		int colHasAlarm = cursor.GetColumnIndex(AndroidEventColumns.HasAlarm);
		int colId = cursor.GetColumnIndex("_id");

		// reading...

		for (uint pageGuard = options.MaxCount; pageGuard > 0; pageGuard--)
		{
			var entry = new Appointment();

			// two properties always present in result
			entry.CalendarId = cursor.GetString(colCalId);
			entry.LocalId = cursor.GetString(colId);

			// rest of properties can be switched off (absent in result set)
			if (colAllDay > -1)
			{
				entry.AllDay = (cursor.GetInt(colAllDay) == 1);
			}
			if (colDetails > -1)
			{
				entry.Details = cursor.GetString(colDetails);
			}
			if (colLocation > -1)
			{
				entry.Location = cursor.GetString(colLocation);
			}

			if (_startTimeRequested)
			{
				entry.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(colStartTime));
			}

			if (_durationRequested)
			{
				// calculate duration from start/end, as Android Duration field sometimes is null, and is in hard to parse RFC2445 format 
				long startTime = cursor.GetLong(colStartTime);
				long endTime = cursor.GetLong(colEndTime);
				entry.Duration = TimeSpan.FromMilliseconds(endTime - startTime);
			}

			if (colSubject > -1)
			{
				entry.Subject = cursor.GetString(colSubject);
			}

			if (colOrganizer > -1)
			{
				var organ = new AppointmentOrganizer();
				organ.Address = cursor.GetString(colOrganizer);
				entry.Organizer = organ;
			}

			if (colHasAlarm > -1)
			{
				// first, set it to default value 
				entry.Reminder = TimeSpan.FromMinutes(DefaultReminderInMinutes);

				// now, search for implicite alarm times (non-default)
				var projectionCols = new string[] {
					Android.Provider.CalendarContract.IRemindersColumns.EventId,
					Android.Provider.CalendarContract.IRemindersColumns.Minutes,
				};

				using var cursor_reminder = Android.Provider.CalendarContract.Reminders.Query(
					contentResolver, cursor.GetLong(colId), projectionCols);

				if (cursor_reminder != null)
				{
					if (cursor_reminder.MoveToFirst())
					{
						int minReminder = int.MaxValue;
						do
						{
							int currMinutes = cursor_reminder.GetInt(1);
							if (currMinutes == -1)
							{
								// == -1 means "default time"
								currMinutes = DefaultReminderInMinutes;
							}
							minReminder = Math.Min(minReminder, currMinutes);
						}
						while (cursor_reminder.MoveToNext());

						if (minReminder == int.MaxValue)
						{
							minReminder = DefaultReminderInMinutes;
						}
						entry.Reminder = TimeSpan.FromMinutes(minReminder);
					}
					cursor_reminder.Close();
				}
			}

			entriesList.Add(entry);

			if (!cursor.MoveToNext())
			{
				break;
			}
		}

		cursor.Close();

		return entriesList;
	}

	private List<string> ConvertWinRTToAndroidColumnNames(IList<string> uwpColumns)
	{
		_startTimeRequested = false;
		_durationRequested = false;

		List<string> androidColumns = new();

		foreach (var column in uwpColumns)
		{
			if (_appointmentPropertyMap.TryGetValue(column, out var androidColumn))
			{
				androidColumns.Add(androidColumn);
			}
			else if (column == AppointmentProperties.StartTime)
			{
				_startTimeRequested = true;
			}
			else if (column == AppointmentProperties.Duration)
			{
				_durationRequested = true;
			}
		}
		return androidColumns;
	}
}
