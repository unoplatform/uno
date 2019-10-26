using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Foundation;
using MessageUI;

namespace Windows.ApplicationModel.Email
{
	public partial class EmailManager
	{
		public static IAsyncAction ShowComposeNewEmailAsync(EmailMessage message)
		{
			if (message == null)
			{
				throw new ArgumentNullException(nameof(message));
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
					message.To.Select(r=>r.Address).ToArray());
			}

			if (message?.CC?.Count > 0)
			{
				controller.SetCcRecipients(
					message.CC.Select(cc=>cc.Address).ToArray());
			}

			if (message?.Bcc?.Count > 0)
			{
				controller.SetBccRecipients(
					message.Bcc.Select(bcc=>bcc.Address).ToArray());
			}

			if (message?.Attachments?.Count > 0)
			{
				foreach (var attachment in message.Attachments)
				{
					var data = NSData.FromFile(attachment.FullPath);
					controller.AddAttachmentData(data, attachment.ContentType, attachment.AttachmentName);
				}
			}

			// show the controller
			var tcs = new TaskCompletionSource<bool>();
			controller.Finished += (sender, e) =>
			{
				controller.DismissViewController(true, null);
				tcs.SetResult(e.Result == MFMailComposeResult.Sent);
			};
			parentController.PresentViewController(controller, true, null);

			return Task.CompletedTask.AsAsyncAction();
		}
	}
}
