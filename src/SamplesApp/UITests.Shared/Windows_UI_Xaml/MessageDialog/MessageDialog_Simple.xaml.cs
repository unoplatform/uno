using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.MessageDialog
{
	[SampleControlInfo("MessageDialog", "MessageDialog_Simple")]
	public sealed partial class MessageDialog_Simple : UserControl
	{
		public MessageDialog_Simple()
		{
			this.InitializeComponent();
		}

		private async void ShowDialog(object sender, RoutedEventArgs e)
		{
			var response = await Show(CancellationToken.None, "Are you sure?", "Confirmation");

			if (response == "Yes")
			{
				Show("Action executed.");
			}
			else
			{
				Show("Action canceled.");
			}
		}

		private async void ShowComplexDialog(object sender, RoutedEventArgs e)
		{
			var choices = new[]
			{
				"Captain Kirk",
				"Han Solo",
				"Neo"
			};

			var dialog = new Windows.UI.Popups.MessageDialog("Which one is your preferred character?", "Quiz");

			try
			{
				for (int i = 0; i < choices.Length; i++)
				{
					dialog.Commands.Add(new UICommand(choices[i], null, i));
				}
			}
			catch (Exception error)
			{
				// On Windows Phone, this will fail. If you add more than three, also on Windows and Android.
				Show(error.Message);
				return;
			}

			dialog.DefaultCommandIndex = 0;

			var response = await dialog.ShowAsync();

			if (response?.Id == null)
			{
				Show("You don't want to commit yourself? Fine...");
			}
			else
			{
				Show($"You chose {choices[(int)response.Id]}, which is a very good pick!");
			}
		}

		private async void ShowDialogs(object sender, RoutedEventArgs e)
		{
			var response1 = await Show(CancellationToken.None, "This is the first question.", "Important");
			var response2 = await Show(CancellationToken.None, "This is the second question.", "Warning", "Ok", "Cancel");
			var response3 = await Show(CancellationToken.None, "This is the final question.", "Error", "Retry", "Cancel");

			Show($"Your responses were {response1}, {response2} and {response3}.");
		}

		private async void ShowDialogsStacked(object sender, RoutedEventArgs e)
		{
			var response1Task = Show(CancellationToken.None, "This is the first question.", "Important");
			var response2Task = Show(CancellationToken.None, "This is the second question.", "Warning", "Ok", "Cancel");
			var response3Task = Show(CancellationToken.None, "This is the final question.", "Error", "Retry", "Cancel");

			await Task.WhenAll(response1Task, response2Task, response3Task);

			Show($"Your responses were {response1Task.Result}, {response2Task.Result} and {response3Task.Result}.");
		}

		// This pattern works on Windows, but it seems AsTack(ct) fails to cancel inner IAsyncOperation on Windows.
		// That's why ShowDialogCancelled2 was made to explicitly test IAsyncOperation.Cancel.
		private async void ShowDialogCancelled(object sender, RoutedEventArgs e)
		{
			var response = default(string);

			try
			{
				using (var source = new CancellationTokenSource(3000))
				{
					response = await Show(source.Token, "This message should automatically close after 3 seconds, except on Windows Phone.", "Please wait");
				}
			}
			catch (OperationCanceledException)
			{
				response = "CANCELLED";
			}

			Show($"The response was {response}.");
		}

		private async void ShowDialogCancelled2(object sender, RoutedEventArgs e)
		{
			var operation = new Windows.UI.Popups.MessageDialog("This message should automatically close after 3 seconds, except on Windows Phone.", "Please wait").ShowAsync();

			await Task.Delay(3000);

			operation.Cancel();
		}

		private async Task<string> Show(CancellationToken ct, string message, string title, string button1Label = "Yes", string button2Label = "No")
		{
			var button1 = new UICommand(button1Label);
			var button2 = new UICommand(button2Label);

			var dialog = new Windows.UI.Popups.MessageDialog(message, title)
			{
				Commands = { button2, button1 },
				// Not setting this, should be the default.
				//DefaultCommandIndex = 0,
				CancelCommandIndex = 0
			};

			var command = await dialog.ShowAsync().AsTask(ct);

			return command?.Label ?? "(null)";
		}

		private async void Show(string message)
		{
			await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
		}
	}
}
