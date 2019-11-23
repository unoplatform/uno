#if __ANDROID__
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Uno.Helpers.Activities;
using Windows.Foundation;

namespace Windows.ApplicationModel.Email
{
	public partial class EmailManager
	{
		private const int HandlerPickerResultCode = 0;

		private static async Task ShowComposeNewEmailInternalAsync(EmailMessage message)
		{
			message = message ?? throw new ArgumentNullException(nameof(message));

			var resolvedComponent = await ResolveEmailComponent();

			var intent = new Intent(Intent.ActionSend)
				.SetFlags(ActivityFlags.ClearTop)
				.SetFlags(ActivityFlags.NewTask)
				.SetType("plain/text");

			if (resolvedComponent != null)
			{
				intent.SetComponent(resolvedComponent);
			}

			if (!string.IsNullOrEmpty(message.Body))
			{
				intent.PutExtra(
					Intent.ExtraText,
					message.Body);
			}

			if (!string.IsNullOrEmpty(message.Subject))
			{
				intent.PutExtra(
					Intent.ExtraSubject,
					message.Subject);
			}

			if (message.To.Count > 0)
			{
				intent.PutExtra(
					Intent.ExtraEmail,
					message.To.Select(m => m.Address).ToArray());
			}

			if (message.CC.Count > 0)
			{
				intent.PutExtra(
					Intent.ExtraCc,
					message.CC.Select(m => m.Address).ToArray());
			}

			if (message.Bcc.Count > 0)
			{
				intent.PutExtra(
					Intent.ExtraBcc,
					message.Bcc.Select(m => m.Address).ToArray());
			}

			Application.Context.StartActivity(intent);
		}

		private static async Task<ComponentName> ResolveEmailComponent()
		{
			var packageManager = Application.Context.PackageManager;

			var emailDummyIntent = new Intent(Intent.ActionSendto);
			emailDummyIntent.SetData(Android.Net.Uri.Parse("mailto:dummy@email.com"));

			var emailActivities = packageManager.QueryIntentActivities(emailDummyIntent, 0);

			if (emailActivities == null)
			{
				return null;
			}

			if (emailActivities.Count == 1)
			{
				return GetComponentName(emailActivities[0]);
			}

			var defaultHandler = emailActivities.FirstOrDefault(e => e.IsDefault);
			if (defaultHandler != null)
			{
				return GetComponentName(defaultHandler);
			}

			var intentPick = new Intent();
			intentPick.SetAction(Intent.ActionPickActivity);
			intentPick.PutExtra(Intent.ExtraIntent, emailDummyIntent);

			var awaitableResultActivity = await AwaitableResultActivity.StartAsync();
			var result = await awaitableResultActivity.StartActivityForResultAsync(intentPick, HandlerPickerResultCode);

			if (result.Result == Result.Ok)
			{
				return result.Intent.Component;
			}
			return null;
		}

		private static ComponentName GetComponentName(ResolveInfo resolve)
		{
			return new ComponentName(resolve.ActivityInfo.PackageName, resolve.ActivityInfo.Name);
		}
	}
}
#endif
