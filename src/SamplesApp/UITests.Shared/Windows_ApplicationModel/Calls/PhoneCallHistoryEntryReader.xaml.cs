using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Calls;
          
namespace UITests.Shared.Windows_ApplicationModel.Calls
{
	[SampleControlInfo("Windows_ApplicationModel.Calls", "ContactReader")]

	public sealed partial class PhoneCallHistoryEntryReader : UserControl
	{

		public PhoneCallHistoryEntryReader()
		{
			this.InitializeComponent();
		}

		private async void getCallLog_Click(object sender, RoutedEventArgs e)
		{
			uiErrorMsg.Text = "";
			uiLastNumber.Text = "";
			uiLastDate.Text = "";
			uiOkMsg.Text = "";
			uiDuration.Text = "";

			Windows.ApplicationModel.Calls.PhoneCallHistoryStore oCallHist;
			try
			{
				oCallHist = await Windows.ApplicationModel.Calls.PhoneCallHistoryManager.RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);
			}
			catch (Exception ex)
			{
				uiErrorMsg.Text = "Exception while RequestStoreAsync: " + ex.Message;
				return;
			}

			if (oCallHist == null)
			{
				uiErrorMsg.Text = "Got null as store from RequestStoreAsync";
				return;
			}

			var oHistReader = oCallHist.GetEntryReader();
			if (oHistReader == null)
			{
				uiErrorMsg.Text = "Got null as reader from GetEntryReader";
				return;
			}

			var oBatch = await oHistReader.ReadBatchAsync();
			if (oBatch == null)
			{
				uiErrorMsg.Text = "Got null as batch from ReadBatchAsync";
				return;
			}

			if (oBatch.Count < 1)
			{
				uiErrorMsg.Text = "Seems like you have no calls logged";
				return;
			}

			uiOkMsg.Text = "First batch contains " + oBatch.Count.ToString() + " contacts, data from first:";

			var entry = oBatch[0];

			uiLastNumber.Text = entry.Address.RawAddress;
			uiLastDate.Text = entry.StartTime.ToString();
			if (entry.Duration.HasValue)
			{
				uiDuration.Text = entry.Duration.ToString("mm:ss");
			}
			else
			{
				uiDuration.Text = "(unknown duration)";
			}

		}

	}
}
