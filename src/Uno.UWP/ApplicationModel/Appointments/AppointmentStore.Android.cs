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
		private bool _StartTimeRequested = false; // if it should be included in UWP output result set
		private bool _DurationRequested = false; // set if duration should be included in UWP output result

		private List<string> UWP2AndroColumnNames(IList<string> uwpColumns)
		{
			_StartTimeRequested = false;

			List<string> androColumns = new List<string>();
			foreach (var column in uwpColumns)
			{
				switch (column)
				{
					case "Appointment.AllDay":
						androColumns.Add(Android.Provider.CalendarContract.EventsColumns.AllDay);
						break;
					case "Appointment.Location":
						androColumns.Add(Android.Provider.CalendarContract.EventsColumns.EventLocation);
						break;
					case "Appointment.StartTime":
						_StartTimeRequested = true;
						break;
					case "Appointment.Duration":
						_DurationRequested = true;
						break;
					case "Appointment.Subject":
						androColumns.Add(Android.Provider.CalendarContract.EventsColumns.Title);
						break;
					case "Appointment.Organizer":
						androColumns.Add(Android.Provider.CalendarContract.EventsColumns.Organizer);
						break;
					case "Appointment.Details":
						androColumns.Add(Android.Provider.CalendarContract.EventsColumns.Description);
						break;
				}
			}
			return androColumns;
		}


		public IAsyncOperation<IReadOnlyList<Appointment>> FindAppointmentsAsync(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
			=> FindAppointmentsAsyncTask(rangeStart, rangeLength, options).AsAsyncOperation<IReadOnlyList<Appointment>>();
		private async Task<IReadOnlyList<Appointment>> FindAppointmentsAsyncTask(DateTimeOffset rangeStart, TimeSpan rangeLength, FindAppointmentsOptions options)
		{
			Android.Database.ICursor _cursor = null;
			Android.Content.ContentResolver _contentResolver = null;
			List<Appointment> entriesList = new List<Appointment>();

			if (options is null)
			{
				throw new ArgumentNullException();
			}

			Android.Net.Uri.Builder builder = Android.Provider.CalendarContract.Instances.ContentUri.BuildUpon();
			Android.Content.ContentUris.AppendId(builder, rangeStart.ToUniversalTime().ToUnixTimeMilliseconds());
			var rangeEnd = rangeStart + rangeLength;
			Android.Content.ContentUris.AppendId(builder, rangeEnd.ToUniversalTime().ToUnixTimeMilliseconds());
			Android.Net.Uri oUri = builder.Build();
			// it is simply: {content://com.android.calendar/instances/when/1588275364371/1588880164371}

			var androColumns = UWP2AndroColumnNames(options.FetchProperties);
			// some 'system columns' columns, cannot be switched off
			androColumns.Add(Android.Provider.CalendarContract.EventsColumns.CalendarId);
			androColumns.Add("_id");
			androColumns.Add(Android.Provider.CalendarContract.Instances.Begin);    // for sort
			androColumns.Add(Android.Provider.CalendarContract.Instances.End);    // we need this, as Android sometimes has NULL duration, and it should be reconstructed from start/end

			// string sortMode = Android.Provider.CalendarContract.EventsColumns.Dtstart + " ASC";
			string sortMode = Android.Provider.CalendarContract.Instances.Begin + " ASC";
			_contentResolver = Android.App.Application.Context.ContentResolver;
			_cursor = _contentResolver.Query(oUri,
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
			int _colAllDay, _colLocation, _colStartTime, _colSubject, _colOrganizer, _colDetails, _colCalId, _colId, _colEndTime;
			_colAllDay = _cursor.GetColumnIndex(Android.Provider.CalendarContract.EventsColumns.AllDay);
			_colLocation = _cursor.GetColumnIndex(Android.Provider.CalendarContract.EventsColumns.EventLocation);
			_colStartTime = _cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.Begin);
			_colEndTime = _cursor.GetColumnIndex(Android.Provider.CalendarContract.Instances.End);
			_colSubject = _cursor.GetColumnIndex(Android.Provider.CalendarContract.EventsColumns.Title);
			_colOrganizer = _cursor.GetColumnIndex(Android.Provider.CalendarContract.EventsColumns.Organizer);
			_colDetails = _cursor.GetColumnIndex(Android.Provider.CalendarContract.EventsColumns.Description);
			_colCalId = _cursor.GetColumnIndex(Android.Provider.CalendarContract.EventsColumns.CalendarId);
			_colId = _cursor.GetColumnIndex("_id");


			// odczytanie

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

				if (_StartTimeRequested)
				{
					entry.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(_cursor.GetLong(_colStartTime));
				}

				if (_DurationRequested)
				{
					// calculate duration from start/end, as Android Duration field sometimes is null, and is in hard to parse RFC2445 format 
					long startTime, endTime;
					startTime = _cursor.GetLong(_colStartTime);
					endTime = _cursor.GetLong(_colEndTime);
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
