#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageUI;
using UIKit;
using Windows.Foundation;

namespace Windows.ApplicationModel.Chat
{
	public partial class ChatMessageManager
	{
		public static IAsyncAction ShowComposeSmsMessageAsync(ChatMessage message)
		{
			if (message == null)
			{
				throw new ArgumentNullException(nameof(message));
			}

			var window = UIApplication.SharedApplication.KeyWindow;
			var controller = window.RootViewController;

			var messageController = new MFMessageComposeViewController();

			messageController.Body = message.Body;
			messageController.Recipients = message?.Recipients?.ToArray() ?? new string[] { };

			var tcs = new TaskCompletionSource<bool>();
			messageController.Finished += (sender, e) =>
			{
				messageController.DismissViewController(true, null);
				tcs.SetResult(e.Result == MessageComposeResult.Sent);
			};

			controller.PresentViewController(messageController, true, null);

			return tcs.Task.AsAsyncAction();
		}
	}
}
#endif
