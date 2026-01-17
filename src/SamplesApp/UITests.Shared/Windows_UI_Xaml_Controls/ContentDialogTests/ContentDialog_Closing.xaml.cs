using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using _Button = Microsoft.UI.Xaml.Controls.Button;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests
{
	[Sample("Dialogs", "ContentDialog_Closing", Description: "Tests for ContentDialog.Closing event")]
	public sealed partial class ContentDialog_Closing : UserControl
	{
		public ContentDialog_Closing()
		{
			this.InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs args)
		{
			DidCloseTextBlock.Text = "Not closed";
			var dialog = new ContentDialog { CloseButtonText = "Close" };

			dialog.Closing += (o, e) =>
			{
				ResultTextBlock.Text = "Closing event was raised!";
			};

			dialog.Closed += (o, e) =>
			{
				DidCloseTextBlock.Text = "Closed";
			};

			dialog.XamlRoot = this.XamlRoot;
			var dummy = dialog.ShowAsync();

			await Task.Delay(50);
			dialog.Hide();
		}

		private void DeferredDialog_Click(object sender, RoutedEventArgs args)
		{
			DidCloseTextBlock.Text = "Not closed";
			var dialog = new ContentDialog { CloseButtonText = "Close" };
			ContentDialogClosingDeferral deferral1 = null;
			ContentDialogClosingDeferral deferral2 = null;
			var defer1Button = new _Button { Name = "Complete1Button", Content = "Complete 1" };
			defer1Button.Click += (o, e) =>
			{
				ResultTextBlock.Text = "First complete called";
				deferral1.Complete();
			};
			var defer2Button = new _Button { Name = "Complete2Button", Content = "Complete 2" };
			defer2Button.Click += (o, e) =>
			{
				ResultTextBlock.Text = "Second complete called";
				deferral2.Complete();
			};
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Children = {
					defer1Button,
					defer2Button
				}
			};
			dialog.Content = panel;

			dialog.Closing += (o, e) =>
			{
				deferral1 = e.GetDeferral();
			};
			dialog.Closing += (o, e) =>
			{
				deferral2 = e.GetDeferral();
			};

			dialog.Closed += (o, e) =>
			{
				DidCloseTextBlock.Text = "Closed";
			};

			dialog.XamlRoot = this.XamlRoot;
			var dummy = dialog.ShowAsync();
		}

		private void PrimaryDialog_Click(object sender, RoutedEventArgs args)
		{
			DidCloseTextBlock.Text = "Not closed";
			var dialog = new ContentDialog { CloseButtonText = "Close", PrimaryButtonText = "Primo" };
			dialog.Closing += (o, e) =>
			  {
				  ResultTextBlock.Text = e.Result.ToString();
			  };

			dialog.Closed += (o, e) =>
			{
				DidCloseTextBlock.Text = "Closed";
			};

			dialog.XamlRoot = this.XamlRoot;
			var dummy = dialog.ShowAsync();
		}

		private void PrimaryDialogCancelClosing_Click(object sender, RoutedEventArgs args)
		{
			DidCloseTextBlock.Text = "Not closed";
			var dialog = new ContentDialog { CloseButtonText = "Close", PrimaryButtonText = "Primo" };
			dialog.Closing += (o, e) =>
			  {
				  ResultTextBlock.Text = e.Result.ToString();

				  if (e.Result == ContentDialogResult.Primary)
				  {
					  e.Cancel = true;
				  }
			  };

			dialog.Closed += (o, e) =>
			{
				DidCloseTextBlock.Text = "Closed";
			};

			dialog.XamlRoot = this.XamlRoot;
			var dummy = dialog.ShowAsync();
		}
	}
}
