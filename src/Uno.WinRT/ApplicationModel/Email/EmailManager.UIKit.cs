#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using UIKit;
using System.Net;
using System.Collections.Generic;
using Foundation;
using Windows.System;

#if !__TVOS__
using MessageUI;
#endif

namespace Windows.ApplicationModel.Email
{
	public partial class EmailManager
	{
		private static async Task ShowComposeNewEmailInternalAsync(EmailMessage message)
		{
			ArgumentNullException.ThrowIfNull(message);

#if !__TVOS__
			if (Uno.WinRTFeatureConfiguration.EmailManager.UseMailAppAsDefaultEmailClient &&
				MFMailComposeViewController.CanSendMail)
			{
				await ComposeEmailWithMFAsync(message);
			}
			else
#endif
			{
				//fallback when user hasn't set up e-mail account yet
				await ComposeEmailWithMailtoUriAsync(message);
			}
		}

#if !__TVOS__
		private static async Task ComposeEmailWithMFAsync(EmailMessage message)
		{
			if (UIApplication.SharedApplication.KeyWindow?.RootViewController is not { } rootViewController)
			{
				throw new InvalidOperationException("Root view controller is null, API called too early in the application lifecycle.");
			}

			var controller = new MFMailComposeViewController
			{
				MailComposeDelegate = new MailComposeViewControllerDelegate()
			};

			if (message is { } msg)
			{
				if (!string.IsNullOrEmpty(msg.Body))
				{
					controller.SetMessageBody(msg.Body, true);
				}

				if (!string.IsNullOrEmpty(msg.Subject))
				{
					controller.SetSubject(msg.Subject);
				}

				if (msg.To.Count > 0)
				{
					controller.SetToRecipients(msg.To.Select(r => r.Address).ToArray());
				}

				if (msg.CC.Count > 0)
				{
					controller.SetCcRecipients(msg.CC.Select(cc => cc.Address).ToArray());
				}

				if (msg.Bcc.Count > 0)
				{
					controller.SetBccRecipients(msg.Bcc.Select(bcc => bcc.Address).ToArray());
				}
			}

			await rootViewController.PresentViewControllerAsync(controller, true);
		}
#endif
	}

#if !__TVOS__
	public class MailComposeViewControllerDelegate : MFMailComposeViewControllerDelegate
	{
		public override void Finished(MFMailComposeViewController controller, MFMailComposeResult result, NSError? error)
		{
			controller.DismissViewController(true, null);
		}
	}
#endif
}
