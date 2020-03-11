#nullable enable

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
			=> SaveMessageAsyncTaskProxy(chatMessage).AsAsyncAction();

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

#region Changing default SMS app

		internal const int RequestCode = 7001;
		private static string _previousDefaultApp = "";
		private static TaskCompletionSource<Android.Content.Intent?>? _currentSwitchSMSappRequest;

		private string GetCurrentDefaultSMSapp()
		{
			var context = Android.App.Application.Context;
			string? appName =  Android.Provider.Telephony.Sms.GetDefaultSmsPackage(context);
			if(appName is null)
			{
				return "";
			}
			return appName;
		}

		/// <summary>
		///  Switch default SMS app to newAppName (as in context.PackageName)
		///  Return false if switching is unsuccesful.
		///  To be "candidate" for default SMS app, your app must define several services/activities.
		///  See https://stackoverflow.com/questions/21720657/how-to-set-my-sms-app-default-in-android-kitkat
		/// </summary>
		public async Task<bool> SwitchDefaultSMSapp(string newAppName)
		{
			if (!(Uno.UI.ContextHelper.Current is Android.App.Activity appActivity))
			{
				throw new InvalidOperationException("Application activity is not yet set, SwitchDefaultSMSapp called too early.");
			}

			if(GetCurrentDefaultSMSapp() == newAppName)
			{
				return true;
			}

			Android.Content.Intent intent;

			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.K)
			{
				throw new NotImplementedException("OS.Build SDK is too low (I can work with  API > 18, Android Kitkat or newer).");
			}

			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q)
			{
				// works since API 19 (Kitkat, October 2013) till API 28
				// https://developer.android.com/reference/android/provider/Telephony.Sms.Intents
				intent = new Android.Content.Intent(Android.Provider.Telephony.Sms.Intents.ActionChangeDefault);
				intent.PutExtra(Android.Provider.Telephony.Sms.Intents.ExtraPackageName, newAppName);

			}
			else
			{
				//string roleName = Android.App.Roles.RoleManager.RoleSms;
				// below should work (in Android), but doesn't work in Xamarin
				// var intent = Android.App.Roles.RoleManager.createRequestRoleIntent(roleName);

				throw new InvalidOperationException("Android Q+ and Mono/Xamarin API definition mismatch.");

			}

			_currentSwitchSMSappRequest = new TaskCompletionSource<Android.Content.Intent?>();

			appActivity.StartActivityForResult(intent, RequestCode);
			var resultIntent = await _currentSwitchSMSappRequest.Task;
			_currentSwitchSMSappRequest = null;

			// check result (switch successful?)
			if (GetCurrentDefaultSMSapp() == newAppName)
				return true;

			return false; // zmiana nie zostala dokonana
		}

		internal static bool TryHandleIntent(Android.Content.Intent intent, Android.App.Result resultCode)
		{
			if (_currentSwitchSMSappRequest == null)
			{
				return false;
			}
			if (resultCode == Android.App.Result.Canceled)
			{
				_currentSwitchSMSappRequest.SetResult(null);
			}
			else
			{
				_currentSwitchSMSappRequest.SetResult(intent);
			}
			return true;
		}

		/// <summary>
		///  Switch default SMS app to your app (if toThisApp == true), with storing current default app.
		///  Call with toThisApp == false, to restore default SMS app.
		///  Return false if switching is unsuccesful.
		///  To be "candidate" for default SMS app, your app must define several services/activities.
		///  See https://stackoverflow.com/questions/21720657/how-to-set-my-sms-app-default-in-android-kitkat
		/// </summary>
		public async Task<bool> SwitchDefaultSMSapp(bool toThisApp)
		{
			// https://android-developers.googleblog.com/2013/10/getting-your-sms-apps-ready-for-kitkat.html
			var context = Android.App.Application.Context;

			// only the app that receives the SMS_DELIVER_ACTION broadcast (the user-specified default SMS app) is able to write to the SMS Provider

			if (toThisApp)
			{
				string currSmsApp = GetCurrentDefaultSMSapp();
				// if switch to self, and we already are default app
				if (currSmsApp == context.PackageName)
				{
					return true;
				}

				_previousDefaultApp = currSmsApp;

				string? myAppName = context.PackageName;
				if(myAppName is null)
				{
					throw new InvalidOperationException("context.PackageName cannot be null!");
				}

				return await SwitchDefaultSMSapp(myAppName);

			}
			else
			{
				if (string.IsNullOrEmpty(_previousDefaultApp))
					return false;   // error: probably cant happen
				if (_previousDefaultApp == context.PackageName)
					return true;    // if previous app is this app, we have nothing to do

				return await SwitchDefaultSMSapp(_previousDefaultApp);

			}
		}



		#endregion

		private async Task SaveMessageAsyncTaskProxy(Windows.ApplicationModel.Chat.ChatMessage chatMessage)
		{ // do not block calling thread 
			await Task.Run(() => SaveMessageAsyncTask(chatMessage));
		}

		/// <summary>
		///  On Android, to use this method (write to message store) app have to be set as default SMS app.
		///  To be "candidate" for default SMS app, your app must define several services/activities.
		///  See https://android-developers.googleblog.com/2013/10/getting-your-sms-apps-ready-for-kitkat.html
		///  For switching default SMS app, you can use ChatMessageStore.SwitchDefaultSMSapp method.
		/// </summary>
		private async Task SaveMessageAsyncTask(Windows.ApplicationModel.Chat.ChatMessage chatMessage)
		{
			// https://android.googlesource.com/platform/frameworks/opt/telephony/+/f39de086fddea9e9f6b8c56b04d8dd38a84237db/src/java/android/provider/Telephony.java

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
						newSMS.Put(TextBasedSmsColumns("Type"), 4);
						break;
					case Windows.ApplicationModel.Chat.ChatMessageStatus.Sent:
						newSMS.Put(TextBasedSmsColumns("Status"), (int)Android.Provider.SmsStatus.Complete);
						newSMS.Put(TextBasedSmsColumns("Type"), 2);
						break;
					default:
						newSMS.Put(TextBasedSmsColumns("Status"), (int)Android.Provider.SmsStatus.Pending);
						newSMS.Put(TextBasedSmsColumns("Type"), 4);
						break;
				}
			}

			long msecs = chatMessage.LocalTimestamp.ToUnixTimeMilliseconds();
			newSMS.Put(TextBasedSmsColumns("Date"), msecs);
			msecs = chatMessage.NetworkTimestamp.ToUnixTimeMilliseconds();
			newSMS.Put(TextBasedSmsColumns("DateSent"), msecs);

			// 3. insert into Uri
			var context = Android.App.Application.Context;
			if(context.ContentResolver is null)
			{
				throw new InvalidOperationException("context.ContentResolver cannot be null!");

			}
			Android.Content.ContentResolver cr = context.ContentResolver;
			Android.Net.Uri? contentUri = Android.Provider.Telephony.Sms.ContentUri;
			if (contentUri is null)
			{
				throw new InvalidOperationException("Telephony.Sms.ContentUri cannot be null!");

			}
			var retVal = cr.Insert(contentUri, newSMS);
			if (retVal is null)
			{
				// Android reports error, but how to deal with this? UWP API doesn't have similar error...
				// (and, on UWP Desktop, inserting SMS doesn't fail, but SMS is not inserted into ChatStore)
			}
		}
	}

}
