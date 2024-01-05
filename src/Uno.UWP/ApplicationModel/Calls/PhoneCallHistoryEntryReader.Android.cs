#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Provider;
using Windows.Foundation;

namespace Windows.ApplicationModel.Calls;

/// <summary>
/// Enables the calling app to read through the phone call history entries.
/// </summary>
/// <remarks>
/// Implementations follows https://developer.samsung.com/galaxy/others/calllogs-in-android
/// </remarks>
public partial class PhoneCallHistoryEntryReader
{
	private const int PageSize = 100;

	private Android.Database.ICursor? _cursor;

	public PhoneCallHistoryEntryReader() => InitializeCursor();

	~PhoneCallHistoryEntryReader() => CleanupCursor();

	/// <summary>
	/// Returns a list of the PhoneCallHistoryEntry objects.
	/// </summary>
	/// <returns></returns>
	public IAsyncOperation<IReadOnlyList<PhoneCallHistoryEntry>> ReadBatchAsync() => ReadBatchAsyncTask().AsAsyncOperation();

	private Task<IReadOnlyList<PhoneCallHistoryEntry>> ReadBatchAsyncTask()
	{
		var entriesList = new List<PhoneCallHistoryEntry>();

		if (_cursor is null)
		{
			return Task.FromResult<IReadOnlyList<PhoneCallHistoryEntry>>(entriesList);
		}

		try
		{
			// get column indexes, for use within loop
			int callTypeColumn = _cursor.GetColumnIndex(CallLog.Calls.Type);
			int callDurationColumn = _cursor.GetColumnIndex(CallLog.Calls.Duration);
			int callDateColumn = _cursor.GetColumnIndex(CallLog.Calls.Date);
			int callNumberColumn = _cursor.GetColumnIndex(CallLog.Calls.Number);
			int callDisplayNameColumn = _cursor.GetColumnIndex(CallLog.Calls.CachedName);

			for (int entryId = 0; entryId < PageSize; entryId++)
			{
				var entry = new PhoneCallHistoryEntry();

				int callType = _cursor.GetInt(callTypeColumn);
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
				entry.Duration = TimeSpan.FromSeconds(_cursor.GetLong(callDurationColumn));

				// https://developer.samsung.com/galaxy/others/calllogs-in-android - miliseconds
				entry.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(_cursor.GetLong(callDateColumn));

				entry.Address = new PhoneCallHistoryEntryAddress(
					_cursor.GetString(callNumberColumn)!,
					PhoneCallHistoryEntryRawAddressKind.PhoneNumber);

				entry.Address.DisplayName = _cursor.GetString(callDisplayNameColumn) ?? "";

				entriesList.Add(entry);

				if (!_cursor.MoveToNext())
				{
					CleanupCursor();

					break;
				}
			}
		}
		catch
		{
			CleanupCursor();
		}

		return Task.FromResult<IReadOnlyList<PhoneCallHistoryEntry>>(entriesList);
	}

	private void InitializeCursor()
	{
		var contentResolver = Android.App.Application.Context.ContentResolver;
		if (contentResolver is null)
		{
			throw new InvalidOperationException("ContentResolver is null");
		}

		var contentUri = Android.Provider.CallLog.Calls.ContentUri;
		if (contentUri is null)
		{
			throw new InvalidOperationException("ContentUri is null");
		}

		_cursor = contentResolver.Query(contentUri, null, null, null, Android.Provider.CallLog.Calls.DefaultSortOrder); // Ordered by descending date.

		if (_cursor is null)
		{
			throw new InvalidOperationException("Call log cursor is null");
		}

		if (!_cursor.MoveToFirst())
		{
			_cursor.Dispose();
			_cursor = null;
		}
	}

	private void CleanupCursor()
	{
		if (_cursor != null)
		{
			_cursor.Close();
			_cursor.Dispose();
			_cursor = null;
		}
	}
}
