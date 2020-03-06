#if __IOS__
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using MessageUI;
using UIKit;
using System.Net;
using System.Collections.Generic;
using Foundation;
using Windows.System;

namespace Windows.ApplicationModel.Email
{
	public partial class EmailManager
	{
		private static async Task ShowComposeNewEmailInternalAsync(EmailMessage message)
		{
			if (message == null)
			{
				throw new ArgumentNullException(nameof(message));
			}
			
			if (MFMailComposeViewController.CanSendMail)
			{
				await ComposeEmailWithMFAsync(message);
			}
			else
			{
				//fallback when user hasn't set up e-mail account yet
				await ComposeEmailWithMailtoUriAsync(message);
			}			
		}

		private static async Task ComposeEmailWithMFAsync(EmailMessage message)
		{
			if (UIApplication.SharedApplication.KeyWindow?.RootViewController == null)
			{
				throw new InvalidOperationException("Root view controller is null, API called too early in the application lifecycle.");
			}

			var controller = new MFMailComposeViewController();

			if (!string.IsNullOrEmpty(message?.Body))
			{
				controller.SetMessageBody(message.Body, true);
			}

			if (!string.IsNullOrEmpty(message?.Subject))
			{
				controller.SetSubject(message.Subject);
			}

			if (message?.To?.Count > 0)
			{
				controller.SetToRecipients(
					message.To.Select(r => r.Address).ToArray());
			}

			if (message?.CC?.Count > 0)
			{
				controller.SetCcRecipients(
					message.CC.Select(cc => cc.Address).ToArray());
			}

			if (message?.Bcc?.Count > 0)
			{
				controller.SetBccRecipients(
					message.Bcc.Select(bcc => bcc.Address).ToArray());
			}

			await UIApplication.SharedApplication.KeyWindow?.RootViewController.PresentViewControllerAsync(controller, true);
			await controller.DismissViewControllerAsync(true);
		}		
	}
}
#endif
