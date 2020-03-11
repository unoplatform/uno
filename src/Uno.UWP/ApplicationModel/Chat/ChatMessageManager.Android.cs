#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using Android.Provider;
using Uno.UI;
using Windows.Foundation;

namespace Windows.ApplicationModel.Chat
{
	public partial class ChatMessageManager
	{
		private const string SmsProtocol = "smsto:";
		private const string SmsBodyExtra = "sms_body";
		private const string RecipientsSeparator = ";";

		public static IAsyncAction ShowComposeSmsMessageAsync(ChatMessage message)
		{
			if (message == null)
			{
				throw new ArgumentNullException(nameof(message));
			}

			var intent = CreateIntent(message.Body, message.Recipients.ToArray())
				.SetFlags(ActivityFlags.ClearTop)
				.SetFlags(ActivityFlags.NewTask);

			ContextHelper.Current.StartActivity(intent);

			return Task.FromResult(true).AsAsyncAction();
		}

		private static Intent CreateIntent(string body, string[] recipients)
		{
			var intent = new Intent(Intent.ActionView);

			if (!string.IsNullOrWhiteSpace(body))
			{
				intent.PutExtra(SmsBodyExtra, body);
			}

			var recipientUri = string.Join(
				RecipientsSeparator,
				recipients.Select(r => Android.Net.Uri.Encode(r)));
			var uri = Android.Net.Uri.Parse($"{SmsProtocol}{recipientUri}");

			intent.SetData(uri);

			return intent;
		}

		public static IAsyncOperation<ChatMessageStore> RequestStoreAsync()
			=> RequestStoreAsyncTask().AsAsyncOperation<ChatMessageStore>();

		public static async Task<ChatMessageStore> RequestStoreAsyncTask()
		{
			string permission;
			if(Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.WriteSms))
			{
				permission = Android.Manifest.Permission.WriteSms;
			}
			else
			{
				if (Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadSms))
				{
					permission = Android.Manifest.Permission.ReadSms;
				}
				else
				{
					throw new Exception("ChatMessageManager.RequestStoreAsync requires ReadSms or WriteSms permission defined in Android Manifest");
				}
			}

			if(!await Windows.Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, permission))
			{
				if(!await Windows.Extensions.PermissionsHelper.TryGetPermission(CancellationToken.None, permission))
				{
					return null;    // no permission declared in Manifest
				}
			}

			return new ChatMessageStore();
		}

	}
}
#endif
