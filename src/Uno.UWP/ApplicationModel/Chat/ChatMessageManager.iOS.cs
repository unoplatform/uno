using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Windows.Foundation;

#if !__MACCATALYST__ // catalyst https://github.com/xamarin/xamarin-macios/issues/13935
using MessageUI;
#endif

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

#if __MACCATALYST__ // catalyst https://github.com/xamarin/xamarin-macios/issues/13935
			throw new InvalidOperationException("Not supported on catalyst (https://github.com/xamarin/xamarin-macios/issues/13935)");
#else
			var window = UIApplication.SharedApplication.KeyWindow;
			var controller = window!.RootViewController!;

			var messageController = new MFMessageComposeViewController();

			messageController.Body = message.Body;
			messageController.Recipients = message?.Recipients?.ToArray() ?? Array.Empty<string>();

			messageController.Finished += (sender, e) =>
			{
				messageController.DismissViewController(true, null);
			};

			controller.PresentViewController(messageController, true, null);

			return Task.FromResult(true).AsAsyncAction();
#endif
		}
	}
}
