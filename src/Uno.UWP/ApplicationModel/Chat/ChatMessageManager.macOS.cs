using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppKit;
using Foundation;
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

			var firstNumber = message.Recipients.First();
			var uri = $"sms:{firstNumber}";

			if(!string.IsNullOrEmpty(message.Body))
			{
				uri += $"&body={Uri.EscapeDataString(message.Body)}";
			}

			var result = NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(uri));

			return Task.FromResult(result).AsAsyncAction();
		}
	}
}
