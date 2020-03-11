using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Chat
{
	public partial class ChatMessageStore
	{
		public ChatMessageReader GetMessageReader() => new ChatMessageReader(new TimeSpan(36500, 0, 0, 0));
		public ChatMessageReader GetMessageReader(TimeSpan recentTimeLimit) => new ChatMessageReader(recentTimeLimit);
		public IAsyncAction SaveMessageAsync(Windows.ApplicationModel.Chat.ChatMessage chatMessage)
			=> SaveMessageAsyncTask(chatMessage).AsAsyncAction();

		internal static string TextBasedSmsColumns(string name)
		{
			switch (name)
			{ // two versions. Google doesn't change Android API, but Xamarin Android has.
#if __ANDROID_11__
				case "Body":
					return Android.Provider.Telephony.ITextBasedSmsColumns.Body;
				case "Type":
					return Android.Provider.Telephony.ITextBasedSmsColumns.Type;
				case "Read":
					return Android.Provider.Telephony.ITextBasedSmsColumns.Read;
				case "Seen":
					return Android.Provider.Telephony.ITextBasedSmsColumns.Seen;
				case "Status":
					return Android.Provider.Telephony.ITextBasedSmsColumns.Status;
				case "Address":
					return Android.Provider.Telephony.ITextBasedSmsColumns.Address;
				case "Date":
					return Android.Provider.Telephony.ITextBasedSmsColumns.Date;
				case "DateSent":
					return Android.Provider.Telephony.ITextBasedSmsColumns.DateSent;
#else
				case "Body":
					return Android.Provider.Telephony.TextBasedSmsColumns.Body;
				case "Type":
					return Android.Provider.Telephony.TextBasedSmsColumns.Type;
				case "Read":
					return Android.Provider.Telephony.TextBasedSmsColumns.Read;
				case "Seen":
					return Android.Provider.Telephony.TextBasedSmsColumns.Seen;
				case "Status":
					return Android.Provider.Telephony.TextBasedSmsColumns.Status;
				case "Address":
					return Android.Provider.Telephony.TextBasedSmsColumns.Address;
				case "Date":
					return Android.Provider.Telephony.TextBasedSmsColumns.Date;
				case "DateSent":
					return Android.Provider.Telephony.TextBasedSmsColumns.DateSent;
#endif
				default:
					throw new ArgumentException("TextBasedSmsColumns called with unknown column name");
			}
		}

		private async Task SaveMessageAsyncTask(Windows.ApplicationModel.Chat.ChatMessage chatMessage)
		{
			// 1. maybe test permission (for write)?
			var currSmsApp = Android.Provider.Telephony.Sms.GetDefaultSmsPackage(Android.App.Application.Context);
			if (currSmsApp != Android.App.Application.Context.PackageName)
			{
				throw new UnauthorizedAccessException("ChatMessageStore: only app selected as default SMS app can write do SMS store");
			}

			// 2. new SMS
			Android.Content.ContentValues newSMS = new Android.Content.ContentValues();

			newSMS.Put(TextBasedSmsColumns("Body"), chatMessage.Body);
			if (chatMessage.IsIncoming)
			{
				newSMS.Put(TextBasedSmsColumns("Type"), 1);
				newSMS.Put(TextBasedSmsColumns("Read"), chatMessage.IsRead ? 1 : 0);
				newSMS.Put(TextBasedSmsColumns("Seen"), chatMessage.IsSeen ? 1 : 0);
				newSMS.Put(TextBasedSmsColumns("Status"), (int)Android.Provider.SmsStatus.None);
				newSMS.Put(TextBasedSmsColumns("Address"), chatMessage.From);
			}
			else
			{
				newSMS.Put(TextBasedSmsColumns("Type"), 4);
				newSMS.Put(TextBasedSmsColumns("Read"), 1);
				newSMS.Put(TextBasedSmsColumns("Seen"), 1);

				if (chatMessage.Recipients.Count > 0)
				{
					newSMS.Put(TextBasedSmsColumns("Address"), chatMessage.Recipients.ElementAt(0));
				}

				switch (chatMessage.Status)
				{
					case Windows.ApplicationModel.Chat.ChatMessageStatus.ReceiveDownloadFailed:
					case Windows.ApplicationModel.Chat.ChatMessageStatus.SendFailed:
						newSMS.Put(TextBasedSmsColumns("Status"), (int)Android.Provider.SmsStatus.Failed);
						break;
					default:
						newSMS.Put(TextBasedSmsColumns("Status"), (int)Android.Provider.SmsStatus.Pending);
						break;
				}
			}

			long msecs = chatMessage.LocalTimestamp.ToUnixTimeMilliseconds();
			newSMS.Put(TextBasedSmsColumns("Date"), msecs);
			msecs = chatMessage.NetworkTimestamp.ToUnixTimeMilliseconds();
			newSMS.Put(TextBasedSmsColumns("DateSent"), msecs);

			// 3. insert into Uri
			Android.Content.ContentResolver cr = Android.App.Application.Context.ContentResolver;
			var retVal = cr.Insert(Android.Provider.Telephony.Sms.ContentUri, newSMS);
			if (retVal is null)
			{
				// Android reports error, but how to deal with this? UWP API doesn't have similar error...
				// (and, on UWP Desktop, inserting SMS doesn't fail, but SMS is not inserted into ChatStore)
			}
		}
	}

}
