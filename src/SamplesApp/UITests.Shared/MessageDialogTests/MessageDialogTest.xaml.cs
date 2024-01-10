using System;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.MessageDialogTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Dialogs")]
	public sealed partial class MessageDialogTest : Page
	{
		private IAsyncOperation<IUICommand> _asyncOperation;

		public MessageDialogTest()
		{
			this.InitializeComponent();
		}

		private async void OnClick(object sender, RoutedEventArgs e)
		{
			var dialog = new Windows.UI.Popups.MessageDialog("Content", "Title");
			_asyncOperation = dialog.ShowAsync();
			_ = await _asyncOperation;
		}

		private void OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			_asyncOperation.Cancel();
		}
	}
}
