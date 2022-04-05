#nullable enable

#if __ANDROID__
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
		private bool _startTimeRequested = false; // if it should be included in UWP output result set
		private bool _durationRequested = false; // set if duration should be included in UWP output result

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
				}
			}
			return androidColumns;
		}

		public IAsyncOperation<IReadOnlyList<Appointment>> FindAppointmentsAsync(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
			=> FindAppointmentsAsyncTask(rangeStart, rangeLength, options).AsAsyncOperation<IReadOnlyList<Appointment>>();

		private async Task<IReadOnlyList<Appointment>> FindAppointmentsAsyncTask(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
		{
			List<Appointment> entriesList = new List<Appointment>();

			if (options is null)
			{
				throw new ArgumentNullException("Windows.ApplicationModel.Appointments.AppointmentStore.FindAppointmentsAsyncTask, options cannot be null");
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

			using var _cursor = _contentResolver?.Query(oUri,
									androColumns.ToArray(),  // columns in result
									null,   // where
									null,   // where params
									sortMode);
			
			if (_cursor is null || _cursor.IsAfterLast)
			{
				return entriesList;
			}

			if (!_cursor.MoveToFirst())
			{
				return entriesList;
			}

			// optimization
			int _colAllDay = _cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.AllDay);
			int _colLocation = _cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.EventLocation);
			int _colStartTime = _cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.Begin);
			int _colEndTime = _cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.End);
			int _colSubject = _cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Title);
			int _colOrganizer = _cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Organizer);
			int _colDetails = _cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.Description);
			int _colCalId = _cursor.GetColumnIndex(Android.Provider.CalendarContract.IEventsColumns.CalendarId);
			int _colId = _cursor.GetColumnIndex("_id");


			// reading...

			for (uint pageGuard = options.MaxCount; pageGuard > 0; pageGuard--)
			{
				var entry = new Appointment();

				// two properties always present in result
				entry.CalendarId = _cursor.GetString(_colCalId);
				entry.LocalId = _cursor.GetString(_colId);

				// rest of properties can be switched off (absent in result set)
				if (_colAllDay > -1)
				{
					entry.AllDay = (_cursor.GetInt(_colAllDay) == 1);
				}
				if (_colDetails > -1)
				{
					entry.Details = _cursor.GetString(_colDetails);
				}
				if (_colLocation > -1)
				{
					entry.Location = _cursor.GetString(_colLocation);
				}

				if (_startTimeRequested)
				{
					entry.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(_cursor.GetLong(_colStartTime));
				}

				if (_durationRequested)
				{
					// calculate duration from start/end, as Android Duration field sometimes is null, and is in hard to parse RFC2445 format 
					long startTime = _cursor.GetLong(_colStartTime);
					long endTime = _cursor.GetLong(_colEndTime);
					entry.Duration = TimeSpan.FromMilliseconds(endTime - startTime);
				}


				if (_colSubject > -1)
				{
					entry.Subject = _cursor.GetString(_colSubject);
				}

				if (_colOrganizer > -1)
				{
					var organ = new AppointmentOrganizer();
					organ.Address = _cursor.GetString(_colOrganizer);
					entry.Organizer = organ;
				}

				entriesList.Add(entry);

				if (!_cursor.MoveToNext())
				{
					break;
				}
			}

			_cursor.Close();

			return entriesList;

		}


	}
}
#endif
