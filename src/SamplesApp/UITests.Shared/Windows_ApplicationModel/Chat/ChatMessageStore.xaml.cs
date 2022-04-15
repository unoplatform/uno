using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

using uwpChat = Windows.ApplicationModel.Chat;

namespace UITests.Windows_ApplicationModel.Chat
{
	[SampleControlInfo("Windows.ApplicationModel.Chat", "ChatMessageReader")]

	public sealed partial class ChatReader : Page
	{

		public ChatReader()
		{
			this.InitializeComponent();
		}

		private async void getMessages_Click(object sender, RoutedEventArgs e)
		{
			uiErrorMsg.Text = "";

			uwpChat.ChatMessageStore oStore;
			try
			{
				oStore = await uwpChat.ChatMessageManager.RequestStoreAsync();
			}
			catch (Exception ex)
			{
				uiErrorMsg.Text = "Exception while RequestStoreAsync: " + ex.Message;
				return;
			}

			if (oStore == null)
			{
				uiErrorMsg.Text = "Got null as store from RequestStoreAsync";
				return;
			}

			uwpChat.ChatMessageReader oRdr = null;
            try
            {
				oRdr = oStore.GetMessageReader();
			}
            catch (Exception ex)
            {
				uiErrorMsg.Text = "Exception while GetMessageReader: " + ex.Message;
				return;
			}

			var oBatch = await oRdr.ReadBatchAsync();
			if (oContactRdr == null)
			{
				uiErrorMsg.Text = "Got null as batch from ReadBatchAsync";
				return;
			}

			if (oBatch.Count < 1)
			{
				uiErrorMsg.Text = "Seems like you have no SMS stored";
				return;
			}

			uiOkMsg.Text = "First batch contains " + oBatch.Count.ToString() + " messages:";

			var shrtMsgList = new List<ShortSMS>();

			foreach (var oMsg in oBatch)
			{
				var shortSMS = new ShortSMS();

				shortSMS.direction = (oMsg.IsIncoming) ? "inbox" : "outbox";
				shortSMS.sender = oMsg.From;

				string sRecipients = "";
				foreach (string sRcpt in oMsg.Recipients)
				{
					sRecipients = sRecipients + sRcpt + "|";
				}

				if (oMsg.LocalTimestamp != null)
				{
					shortSMS.timeStamp = oMsg.LocalTimestamp.ToString("yyyy.MM.dd HH:mm");
				}

				if (oMsg.Body != null)
				{
					shortSMS.body = oMsg.Body;
				}



				shrtMsgList.Add(shortContact);
			}

			uiListItems.ItemsSource = shrtMsgList.ToArray();

		}
	}


	public class ShortSMS
	{
		public string timeStamp { get; set; }
		public string direction { get; set; }
		public string sender { get; set; }
		public string recipient { get; set; }
		public string body { get; set; }
	}
}
