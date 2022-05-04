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
		// set to `true` if should be included in UWP output result set
		private bool _startTimeRequested = false;
		private bool _durationRequested = false;

		private List<string> UWP2AndroColumnNames(IList<string> uwpColumns)
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

		// I don't know if this is default everywhere, or only on my tablet...
		private int DEFAULT_REMINDER_MINUTES = 15;

		private async Task<IReadOnlyList<Appointment>?> FindAppointmentsAsyncTask(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
		{
			List<Appointment> entriesList = new List<Appointment>();

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
			var oUri = builder.Build();
			// it is simply: {content://com.android.calendar/instances/when/1588275364371/1588880164371}
			if (oUri == null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Appointments.AppointmentStore.FindAppointmentsAsyncTask, oUri is null (impossible)");
			}

			var androColumns = UWP2AndroColumnNames(options.FetchProperties);
			// some 'system columns' columns, cannot be switched off
			androColumns.Add(Android.Provider.CalendarContract.IEventsColumns.CalendarId);
			androColumns.Add("_id");
			androColumns.Add(Android.Provider.CalendarContract.Instances.Begin);    // for sort
			androColumns.Add(Android.Provider.CalendarContract.Instances.End);    // we need this, as Android sometimes has NULL duration, and it should be reconstructed from start/end

			// string sortMode = Android.Provider.CalendarContract.EventsColumns.Dtstart + " ASC";
			var sortMode = Android.Provider.CalendarContract.Instances.Begin + " ASC";
			var _contentResolver = Android.App.Application.Context.ContentResolver;
			if (_contentResolver == null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Appointments.AppointmentStore.FindAppointmentsAsyncTask, _contentResolver is null (impossible)");
			}

			using var cursor = _contentResolver?.Query(oUri,
									androColumns.ToArray(),  // columns in result
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
			int _colAllDay = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.AllDay);
			int _colLocation = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.EventLocation);
			int _colStartTime = cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.Begin);
			int _colEndTime = cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.End);
			int _colSubject = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Title);
			int _colOrganizer = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Organizer);
			int _colDetails = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Description);
			int _colCalId = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.CalendarId);
			int _colHasAlarm = cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.HasAlarm);
			int _colId = cursor.GetColumnIndex("_id");


			// reading...

			for (uint pageGuard = options.MaxCount; pageGuard > 0; pageGuard--)
			{
				var entry = new Appointment();

				// two properties always present in result
				entry.CalendarId = cursor.GetString(_colCalId);
				entry.LocalId = cursor.GetString(_colId);

				// rest of properties can be switched off (absent in result set)
				if (_colAllDay > -1)
				{
					entry.AllDay = (cursor.GetInt(_colAllDay) == 1);
				}
				if (_colDetails > -1)
				{
					entry.Details = cursor.GetString(_colDetails);
				}
				if (_colLocation > -1)
				{
					entry.Location = cursor.GetString(_colLocation);
				}

				if (_startTimeRequested)
				{
					entry.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(_colStartTime));
				}

				if (_durationRequested)
				{
					// calculate duration from start/end, as Android Duration field sometimes is null, and is in hard to parse RFC2445 format 
					long startTime = cursor.GetLong(_colStartTime);
					long endTime = cursor.GetLong(_colEndTime);
					entry.Duration = TimeSpan.FromMilliseconds(endTime - startTime);
				}


				if (_colSubject > -1)
				{
					entry.Subject = cursor.GetString(_colSubject);
				}

				if (_colOrganizer > -1)
				{
					var organ = new AppointmentOrganizer();
					organ.Address = cursor.GetString(_colOrganizer);
					entry.Organizer = organ;
				}

				if(_colHasAlarm > -1)
				{
					// first, set it to default value 
					entry.Reminder = TimeSpan.FromMinutes(DEFAULT_REMINDER_MINUTES);

					// now, search for implicite alarm times (non-default)
					var projectionCols = new string[] {
						Android.Provider.CalendarContract.IRemindersColumns.EventId,
						Android.Provider.CalendarContract.IRemindersColumns.Minutes,
					};

					using var cursor_reminder = Android.Provider.CalendarContract.Reminders.Query(
						_contentResolver, cursor.GetLong(_colId), projectionCols);

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
