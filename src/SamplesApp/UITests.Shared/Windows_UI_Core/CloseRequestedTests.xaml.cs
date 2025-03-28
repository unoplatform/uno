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
			var dialog = new ContentDialog();
			dialog.Title = "Exit";
			dialog.Content = "Are you sure you want to exit?";
			dialog.PrimaryButtonText = "Yes";
			dialog.SecondaryButtonText = "No";
			if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			{
				//cancel close by handling the event
				e.Handled = true;
			}
			deferral.Complete();
		}
	}
}
