#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Windows.UI.Notifications
{
	public partial class ToastNotifier
	{
		private string _channelID = "PlatformUno";
		private Android.App.NotificationManager _notificationManager;

		public ToastNotifier()
		{
			var notifManager = Android.App.NotificationManager.FromContext(Android.App.Application.Context);
			if (notifManager is null)
			{
				throw new InvalidOperationException("ToastNotifier: cannot get _notificationManager");
			}
			_notificationManager = notifManager;

			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
			{   // none of this strings would be visible for user
				var channel = new Android.App.NotificationChannel(_channelID, "PlatformUnoChannel", Android.App.NotificationImportance.Default); ; // deprecated: Android.App.NotificationManager.ImportanceDefault
				_notificationManager.CreateNotificationChannel(channel);
			}

		}

		private string ConvertToastTextToString(string toasttext)
		{
			// this is simple text, e.g. "dummy" or "1"
			if (!toasttext.StartsWith("ms-resource:", StringComparison.Ordinal))
			{
				return toasttext;
			}

			// it is text with prefix, directing us to resources
			toasttext = toasttext.Substring("ms-resource:".Length);

			var retVal = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView()?.GetString(toasttext);
			if (retVal is null)
			{   // we have no such string
				return toasttext;
			}
			return retVal;
		}

		private Android.App.Notification.Builder GetToastBuilder()
		{
			// if API < 11, then another version...
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
			{
				return new Android.App.Notification.Builder(Android.App.Application.Context, _channelID);
			}

#pragma warning disable CS0618 // Type or member is obsolete - compilation is for newer SDK, but runtime is on older SDK
			return new Android.App.Notification.Builder(Android.App.Application.Context);
#pragma warning restore CS0618

		}

		private int GetIconId(string packageName)
		{
			var info = Android.App.Application.Context?.ApplicationContext?.PackageManager?.GetApplicationInfo(packageName, 0);

			int iconId;
			if (info != null)
			{
				iconId = info.Icon;
			}
			else
			{
				// try another method - using more resources (CPU and memory), as Intent is involved
				var context = Android.App.Application.Context;
				if (context?.PackageName is null)
				{
					throw new InvalidOperationException("ToastNotifier:GetIconId, Context.PackageName is null");
				}

				var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName);
				if (intent is null)
				{
					throw new InvalidOperationException("ToastNotifier:GetIconId, GetLaunchIntentForPackage is null");
				}

				var resolveInfo = context.PackageManager?.ResolveActivity(intent, Android.Content.PM.PackageInfoFlags.MatchDefaultOnly);
				if (resolveInfo is null)
				{
					throw new InvalidOperationException("ToastNotifier:GetIconId, ResolveActivity is null");
				}
				iconId = resolveInfo.IconResource;
			}

			// iconId == null means error, but we throw exception in Show method - logic would be more readable
			return iconId;

		}

		private void SetToastTexts(Android.App.Notification.Builder builder, XmlDocument xmlDoc)
		{
			string toastTitleText = "";
			StringBuilder toastText = new("");

			var childToast = xmlDoc.GetElementsByTagName("text");
			if ((childToast != null) && childToast.Count > 0)
			{
				// first text - bigger text (title)

				// to keep compiler happy
				var childNode0 = childToast[0];
				if (childNode0 is null)
				{
					throw new InvalidOperationException("Invalid document notification document"); 
				}
				toastTitleText = ConvertToastTextToString(childNode0.InnerText);

				if (childToast.Count > 1)
				{
					// to keep compiler happy
					var childNode1 = childToast[1];
					if (childNode1 is null)
					{
						throw new InvalidOperationException("Invalid document notification document");
					}
					toastText.Append(ConvertToastTextToString(childNode1.InnerText));

					for (int childIndex = 2; childIndex < childToast.Count; childIndex++)
					{   // in most scenarios, this loop will never iterate
						// separate lines with space and \n: \n as line splitting for newer Android, space - for older

						// to keep compiler happy
						var childNode = childToast[childIndex];
						if (childNode is null)
						{
							throw new InvalidOperationException("Invalid document notification document");
						}
						toastText.Append(" \n" + ConvertToastTextToString(childNode.InnerText));
					}
				}
			}


			// now, we have set toastTitleText, and maybe toastText.

			if (!string.IsNullOrEmpty(toastTitleText))
			{
				builder.SetContentTitle(toastTitleText);
			}

			if (toastText.Length > 0)
			{ // Android toasts doesn't support new line character in standard toasts

				if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
				{
					// too old Android... we can't do anything with it
					builder.SetContentText(toastText.ToString());
				}
				else
				{ // since API 16, we can try to emulate multiline toast's text
					var msgLines = toastText.ToString().Split("\n");
					if (msgLines.Length < 2)
					{
						// single line of text
						builder.SetContentText(toastText.ToString());
					}
					else
					{
						if (msgLines.Length > 6)
						{
							// too many lines for InboxStyle - show in expandable form
							var expandableToast = new Android.App.Notification.BigTextStyle().BigText(toastText.ToString());
							builder.SetStyle(expandableToast);
						}
						else
						{
							// between 2 and 6 lines - use InboxStyle
							var inboxToast = new Android.App.Notification.InboxStyle();
							foreach (var oneLine in msgLines)
							{
								inboxToast.AddLine(oneLine.Trim());
							}

							builder.SetStyle(inboxToast);
						}
					}
				}

				builder.SetContentText(toastText.ToString());
			}
		}

		private void SetToastPriority(Android.App.Notification.Builder builder)
		{
			// for somewhat older Android, we need to set Priority (deprecated in API26 - in newer Android priorities are defined in channels)
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.O)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				builder.SetPriority(0); // PRIORITY_DEFAULT
#pragma warning restore CS0618 // Type or member is obsolete
			}

		}

		private void SetToastTimeout(Android.App.Notification.Builder builder, DateTimeOffset? expirationTime)
		{
			if ((expirationTime != null) && (expirationTime.HasValue))
			{
				int seconds = (int)(expirationTime.Value - DateTime.Now).TotalSeconds;
				if (seconds > 0)
				{
					builder.SetTimeoutAfter(seconds * 1000);
				}
			}
		}

		private Android.App.PendingIntent CreateToastIntent(string packageName, XmlDocument xmlDoc)
		{
			var intentAction = Android.App.Application.Context?.PackageManager?.GetLaunchIntentForPackage(packageName);
			if (intentAction is null)
			{
				throw new InvalidOperationException("ToastNotifier:GetIconId, ResolveActivity is null");
			}

			intentAction.SetPackage(packageName);
			// according to doc:
			// The name must include a package prefix, for example the app com.android.contacts would use names like "com.android.contacts.ShowAll"
			// but it works also in this way, and getting package name in background app can be difficult.
			intentAction.PutExtra("Uno.internal.IntentType", (double)ApplicationModel.Activation.ActivationKind.ToastNotification);

			string toastArgument = "";
			var childToast = xmlDoc.GetElementsByTagName("toast");
			if (childToast.Count == 1)
			{
				var childLaunch = childToast[0]?.Attributes?.GetNamedItem("launch");
				if (childLaunch != null)
				{
					var launchValue = childLaunch.Value;
					if (launchValue is null)
					{
						throw new Exception("It cannot happen, but to keep compiler happy...");
					}
					toastArgument = launchValue;
				}
			}

			intentAction.PutExtra("Uno.internal.ToastArgument", toastArgument);

			// we should have intent.data field different for different toast arguments,
			// else - next intent would overwrite previous.
			// it can be any URI, e.g. http://dummy.com/dummy?arg=  
			var data = Android.Net.Uri.Parse("http://unotask.com/toast?arg=" + Android.Net.Uri.Encode(toastArgument));
			intentAction.SetData(data);

			// 12345: arbitrary number, it can be any value (as it is not used in Uno code anywhere)
			var retVal = Android.App.PendingIntent.GetActivity(Android.App.Application.Context, 12345, intentAction, 0); // 12345=requestCode
			if (retVal is null)
			{
				throw new InvalidOperationException("ToastNotifier:CreateToastIntent, GetActivity is null");
			}

			return retVal;
		}


		public void Show(ToastNotification notification)
		{
			var androidNotification = new Android.App.Notification();
			androidNotification.Category = Android.App.Notification.CategoryMessage;

			Android.App.Notification.Builder builder = GetToastBuilder();

			// we use XmlDocument for retrieving text items and launch attribute
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(notification.Content.GetXml());

			// packageName is used in GetIconId and to create launch intent
			var context = Android.App.Application.Context;
			if (context.PackageName is null)
			{
				throw new InvalidOperationException("ToastNotifier:Show, Context is null");
			}

			string packageName = context.PackageName;

			// extract <text> nodes from XML 
			SetToastTexts(builder, xmlDoc);

			// we have to set icon ("This is the only user-visible content that's required", says doc).
			int iconId = GetIconId(packageName);
			if (iconId == 0)
			{
				throw new InvalidOperationException("ToastNotifier: Unable to get application default Icon, make sure to set one in Android manifest file (application/@android:icon)");
			}
			builder.SetSmallIcon(iconId);

			// for somewhat older Android, we need to set Priority(deprecated in API26 - in newer Android priorities are defined in channels)
			SetToastPriority(builder);

			// mirroring / bridging of notification
			builder.SetLocalOnly((notification.NotificationMirroring == NotificationMirroring.Disabled));

			// expiration time
			SetToastTimeout(builder, notification.ExpirationTime);

			// we want notification time to be shown
			// "For apps targeting Build.VERSION_CODES.N and above, this time is not shown anymore by default and must be opted into by using setShowWhen(boolean)"
			// but SetShowWhen is since API 17...
			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.M)
			{
				builder.SetShowWhen(true);
			}


			// action - opening app from tap on Toast (App.OnActivated)
			var pendingIntent = CreateToastIntent(packageName, xmlDoc);
			builder.SetContentIntent(pendingIntent);

			builder.SetAutoCancel(true);


			// another 'must' - uniq Tag/id pair
			string notifTag = notification.Tag;
			if (string.IsNullOrEmpty(notifTag))
			{
				notifTag = Guid.NewGuid().ToString();
			}

			// everything ready, show notification
			// but, it will disappear when app will be closed

			_notificationManager.Notify(notifTag, 12345, builder.Build());  // 12345: arbitrary number
		}

	}
}

