#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Appointments
{
	public partial class AppointmentStore
	{
		// I don't know if this is default everywhere, or only on my tablet...
		private const int DEFAULT_REMINDER_MINUTES = 15;

		// set to `true` if it should be included in the UWP output result set
		private bool _startTimeRequested;
		private bool _durationRequested;

		private List<string> ConvertWinRTToAndroidColumnNames(IList<string> uwpColumns)
		{
			_startTimeRequested = false;

			List<string> androidColumns = new ();
			foreach (var column in uwpColumns)
			{
				switch (column)
				{
					case "Appointment.AllDay":
						androidColumns.Add(Android.Provider.CalendarContract.IEventsColumns.AllDay);
						break;
					case "Appointment.Location":
						androidColumns.Add(Android.Provider.CalendarContract.IEventsColumns.EventLocation);
						break;
					case "Appointment.StartTime":
						_startTimeRequested = true;
						break;
					case "Appointment.Duration":
						_durationRequested = true;
						break;
					case "Appointment.Subject":
						androidColumns.Add(Android.Provider.CalendarContract.IEventsColumns.Title);
						break;
					case "Appointment.Organizer":
						androidColumns.Add(Android.Provider.CalendarContract.IEventsColumns.Organizer);
						break;
					case "Appointment.Details":
						androidColumns.Add(Android.Provider.CalendarContract.IEventsColumns.Description);
						break;
					case "Appointment.Reminder":
						androidColumns.Add(Android.Provider.CalendarContract.IEventsColumns.HasAlarm);
						break;

				}
			}
			return androidColumns;
		}

		public IAsyncOperation<IReadOnlyList<Appointment>?> FindAppointmentsAsync(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
			=> FindAppointmentsAsyncTask(rangeStart, rangeLength, options).AsAsyncOperation<IReadOnlyList<Appointment>?>();

		private async Task<IReadOnlyList<Appointment>?> FindAppointmentsAsyncTask(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
		{
			List<Appointment> entriesList = new ();

			if (options is null)
			{
				return null;
			}

			var builder = Android.Provider.CalendarContract.Instances.ContentUri?.BuildUpon();
			if(builder == null)
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
			androidColumns.Add(Android.Provider.CalendarContract.IEventsColumns.CalendarId);
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
			int colAllDay = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.AllDay);
			int colLocation = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.EventLocation);
			int colStartTime = cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.Begin);
			int colEndTime = cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.End);
			int colSubject = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Title);
			int colOrganizer = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Organizer);
			int colDetails = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Description);
			int colCalId = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.CalendarId);
			int colHasAlarm = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.HasAlarm);
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

				if(colHasAlarm > -1)
				{
					// first, set it to default value 
					entry.Reminder = TimeSpan.FromMinutes(DEFAULT_REMINDER_MINUTES);

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
									currMinutes = DEFAULT_REMINDER_MINUTES;
								}
								minReminder = Math.Min(minReminder, currMinutes);
							}
							while (cursor_reminder.MoveToNext());

							if(minReminder == int.MaxValue)
							{
								minReminder = DEFAULT_REMINDER_MINUTES;
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


	}
}
