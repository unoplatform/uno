using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Core.Preview;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Core
{
	[SampleControlInfo("Windows.UI.Core")]
	public sealed partial class CloseRequestedTests : Page
    {
        public CloseRequestedTests()
        {
            this.InitializeComponent();
        }

		private void AddHandler_Click(object sender, RoutedEventArgs args)
		{
			SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += CloseRequestedTests_CloseRequested;
		}

		private void RemoveHandler_Click(object sender, RoutedEventArgs args)
		{
			SystemNavigationManagerPreview.GetForCurrentView().CloseRequested -= CloseRequestedTests_CloseRequested;
		}


		private async void CloseRequestedTests_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
		{
			var deferral = e.GetDeferral();
			var dialog = new MessageDialog("Are you sure you want to exit?", "Exit");
			var confirmCommand = new UICommand("Yes");
			var cancelCommand = new UICommand("No");
			dialog.Commands.Add(confirmCommand);
			dialog.Commands.Add(cancelCommand);
			if (await dialog.ShowAsync() == cancelCommand)
			{
				//cancel close by handling the event
				e.Handled = true;
			}
			deferral.Complete();
		}
	}
}
