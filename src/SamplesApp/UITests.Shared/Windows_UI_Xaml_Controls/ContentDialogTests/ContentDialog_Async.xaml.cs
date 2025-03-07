using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using _Button = Windows.UI.Xaml.Controls.Button;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests
{
	[SampleControlInfo("Dialogs", "ContentDialog_Async", description: "Tests for ContentDialog async mechanism")]
	public sealed partial class ContentDialog_Async : UserControl
	{
		private ContentDialog dialog;
		public ContentDialog_Async()
		{
			this.InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs args)
		{
			dialog = new ContentDialog();
			_Button hideButton = new _Button()
			{
				Content = new TextBlock() { Name = "HideButton", Text = "Hide" }
			};
			hideButton.Click += HideButton_Click;
			dialog.Content = hideButton;

			DidShowAsyncReturnTextBlock.Text = "Not Returned";
			dialog.XamlRoot = this.XamlRoot;
			var dummy = await dialog.ShowAsync();
			DidShowAsyncReturnTextBlock.Text = "Returned";
		}

		private void HideButton_Click(object sender, RoutedEventArgs e)
		{
			dialog.Hide();
		}
	}
}
