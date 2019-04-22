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
			
			messageController.Finished += (sender, e) =>
			{
				messageController.DismissViewController(true, null);				
			};

			controller.PresentViewController(messageController, true, null);

			return Task.FromResult(true).AsAsyncAction();
		}
	}
}
#endif
