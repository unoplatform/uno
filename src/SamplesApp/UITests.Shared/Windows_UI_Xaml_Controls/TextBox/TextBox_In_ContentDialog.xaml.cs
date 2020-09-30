using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox")]
	public sealed partial class TextBox_In_ContentDialog : UserControl
	{
		public TextBox_In_ContentDialog()
		{
			this.InitializeComponent();
		}

		private async void ShowSinglelineClick(object sender, RoutedEventArgs e)
		{
			var dialog = new ContentDialog()
			{
				Content = new TextBox(),
				PrimaryButtonText = "OK"
			};
			await dialog.ShowAsync();
		}

		private async void ShowMultilineClick(object sender, RoutedEventArgs e)
		{
			var dialog = new ContentDialog()
			{
				Content = new TextBox() { AcceptsReturn = true },
				PrimaryButtonText = "OK"
			};
			await dialog.ShowAsync();
		}
	}
}
