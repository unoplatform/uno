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
using System.Collections.ObjectModel;

namespace UITests.Shared.Windows_ApplicationModel.Calls
{
	[Sample("Windows.ApplicationModel.Calls", Name = "PhoneCallHistoryManager")]
	public sealed partial class PhoneCallHistoryEntryReaderTests : UserControl
	{
		private PhoneCallHistoryStore _store;
		private PhoneCallHistoryEntryReader _reader;

		public PhoneCallHistoryEntryReaderTests()
		{
			this.InitializeComponent();
		}

		public ObservableCollection<PhoneCallHistoryEntry> History { get; } = new();

		private async void ReadCallLog_Click(object sender, RoutedEventArgs e)
		{
			if (_store is null)
			{
				try
				{
					_store = await PhoneCallHistoryManager.RequestStoreAsync(PhoneCallHistoryStoreAccessType.AllEntriesReadWrite);
				}
				catch (Exception ex)
				{
					uiErrorMsg.Text = "Exception while RequestStoreAsync: " + ex;
					return;
				}
			}

			if (_store is null)
			{
				uiErrorMsg.Text = "Got null as store from RequestStoreAsync";
				return;
			}

			if (_reader is null)
			{
				_reader = _store.GetEntryReader();
			}

			if (_reader is null)
			{
				uiErrorMsg.Text = "Got null as reader from GetEntryReader";
				return;
			}

			var batch = await _reader.ReadBatchAsync();
			if (batch is null)
			{
				uiErrorMsg.Text = "Got null as batch from ReadBatchAsync";
				return;
			}

			if (batch.Count == 0)
			{
				uiErrorMsg.Text = "Seems like you have no calls logged";
				return;
			}

			foreach (var entry in batch)
			{
				History.Add(entry);
			}
		}

	}
}
