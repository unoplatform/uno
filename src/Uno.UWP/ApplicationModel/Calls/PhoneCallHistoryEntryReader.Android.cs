#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Calls
{
	// https://developer.samsung.com/galaxy/others/calllogs-in-android
	public partial class PhoneCallHistoryEntryReader
	{
		// <uses-permission android:name="android.permission.READ_CONTACTS">  ? A nie calllog?

		private Android.Database.ICursor _cursor = null;

		public PhoneCallHistoryEntryReader()
		{
			Android.Content.ContentResolver cr = Android.App.Application.Context.ContentResolver;

			_cursor = cr.Query(Android.Provider.CallLog.Calls.ContentUri,
									null, 
									null,
									null,
									Android.Provider.CallLog.Calls.DefaultSortOrder);	// == date DESC
			if(!_cursor.MoveToFirst())
			{
				_cursor.Dispose();
				_cursor = null;
			}
		}

		~PhoneCallHistoryEntryReader()
		{
			if (_cursor != null)
			{
				_cursor.Close();
				_cursor.Dispose();
				_cursor = null;
			}
		}


		public IAsyncOperation<IReadOnlyList<PhoneCallHistoryEntry>> ReadBatchAsync() => ReadBatchAsyncTask().AsAsyncOperation<IReadOnlyList<PhoneCallHistoryEntry>>();
		private async Task<IReadOnlyList<PhoneCallHistoryEntry>> ReadBatchAsyncTask()
		{

			var entriesList = new List<PhoneCallHistoryEntry>();

			if (_cursor is null)
			{
				return entriesList;
			}

			// get column indexes, for use within loop
			int iColType = _cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Type);
			int iColDuration = _cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Duration);
			int iColStartTime = _cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Date);
			int iColAddress = _cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Number);
			int iColDispName = _cursor.GetColumnIndex(Android.Provider.CallLog.Calls.CachedName);

			for (int pageGuard = 100; pageGuard > 0 ; pageGuard--)
			{
				var entry = new PhoneCallHistoryEntry();

				int callType = _cursor.GetInt(iColType);
				switch (callType)
				{
					case 1:
						entry.IsIncoming = true;
						break;
					case 3:
						entry.IsMissed = true;
						break;
					case 4:
						entry.IsVoicemail = true;
						break;
				}

				// https://developer.android.com/reference/android/provider/CallLog.Calls - seconds
				// https://developer.samsung.com/galaxy/others/calllogs-in-android - miliseconds
				entry.Duration = TimeSpan.FromSeconds(_cursor.GetLong(iColDuration));

				entry.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(_cursor.GetLong(iColStartTime));

				entry.Address = new PhoneCallHistoryEntryAddress(
					_cursor.GetString(iColAddress),
					PhoneCallHistoryEntryRawAddressKind.PhoneNumber);

				try
				{
					entry.Address.DisplayName = _cursor.GetString(iColDispName);
				}
				catch
				{
					// can be null
				}

				entriesList.Add(entry);
				if(! _cursor.MoveToNext())
				{
					_cursor.Close();
					_cursor.Dispose();
					_cursor = null;

					break;
				}
			}

			return entriesList;
		}
	}

}
#endif
