#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Chat
{
	public partial class ChatMessageReader
	{
		// https://stackoverflow.com/questions/848728/how-can-i-read-sms-messages-from-the-device-programmatically-in-android

		private TimeSpan _range;
		private Android.Database.ICursor _cursor = null;
		private int _colBody, _colTime, _colInOut, _colRead, _colSeen, _colAddr, _colStatus;

		internal ChatMessageReader(TimeSpan range)
		{
			_range = range;
			Android.Content.ContentResolver cr = Android.App.Application.Context.ContentResolver;

			_cursor = cr.Query(Android.Provider.Telephony.Sms.ContentUri, null, null, null, Android.Provider.Telephony.Sms.DefaultSortOrder);

			if (_cursor.MoveToFirst())
			{ // runtime optimizations

				_colBody = _cursor.GetColumnIndex(ChatMessageStore.TextBasedSmsColumns("Body"));
				_colTime = _cursor.GetColumnIndex(ChatMessageStore.TextBasedSmsColumns("Date")); //int
				_colInOut = _cursor.GetColumnIndex(ChatMessageStore.TextBasedSmsColumns("Type")); // 1:inbox, 4:outbox, 2:sent, ...
				_colRead = _cursor.GetColumnIndex(ChatMessageStore.TextBasedSmsColumns("Read")); // int (bool)
				_colAddr = _cursor.GetColumnIndex(ChatMessageStore.TextBasedSmsColumns("Address"));
				_colSeen = _cursor.GetColumnIndex(ChatMessageStore.TextBasedSmsColumns("Seen")); // int (bool)
				_colStatus = _cursor.GetColumnIndex(ChatMessageStore.TextBasedSmsColumns("Status")); // int (bool)

/* column numbers, as seen by me on my tablet:
		0: _id
		1: thread_id
		2: address
		3: person
		4: date
		5: protocol
		6: read
		7: status
		8: type
		9: reply_path_present
		10: subject
		11: body
		12: service_center
		13: locked
*/

			}
		}
		~ChatMessageReader()
		{
			if (_cursor != null)
			{
				_cursor.Close();
			}
		}

		public IAsyncOperation<IReadOnlyList<Windows.ApplicationModel.Chat.ChatMessage>> ReadBatchAsync()
			=> ReadBatchAsyncTask().AsAsyncOperation<IReadOnlyList<Windows.ApplicationModel.Chat.ChatMessage>>();

		public async Task<IReadOnlyList<Windows.ApplicationModel.Chat.ChatMessage>> ReadBatchAsyncTask()
		{
			var entriesList = new List<Windows.ApplicationModel.Chat.ChatMessage>();

			if (_cursor is null || _cursor.IsAfterLast)
			{
				return entriesList;
			}

			for (int pageGuard = 100; pageGuard > 0; pageGuard--)
			{
				var entry = new Windows.ApplicationModel.Chat.ChatMessage();

				entry.MessageKind = Windows.ApplicationModel.Chat.ChatMessageKind.Standard;
				entry.MessageOperatorKind = Windows.ApplicationModel.Chat.ChatMessageOperatorKind.Sms;

				entry.Body = _cursor.GetString(_colBody);
				entry.IsIncoming = _cursor.GetInt(_colInOut) == 1;
				entry.LocalTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(_cursor.GetLong(_colTime));
				entry.NetworkTimestamp = entry.LocalTimestamp;
				entry.Recipients.Add(_cursor.GetString(_colAddr));

				if (entry.IsIncoming)
				{
					entry.IsRead = _cursor.GetInt(_colRead) != 0;
					entry.IsSeen = _cursor.GetInt(_colSeen) != 0;
					entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Received;
				}
				else
				{
					entry.IsRead = true;
					entry.IsSeen = true;
					switch (_cursor.GetInt(_colStatus))
					{
						case (int)Android.Provider.SmsStatus.Complete:
						case (int)Android.Provider.SmsStatus.None:
							entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Sent;
							break;
						case (int)Android.Provider.SmsStatus.Failed:
							entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.SendFailed;
							break;
						case (int)Android.Provider.SmsStatus.Pending:
							entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Sending;
							break;
					}
				}

				entriesList.Add(entry);

				if (!_cursor.MoveToNext())
				{
					_cursor.Close();
					_cursor = null;
					break;
				}
			}

			return entriesList;
		}
	}
}

#endif 
