using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Common;
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

using ICommand = System.Windows.Input.ICommand;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests
{
	[SampleControlInfo("ContentDialog", "ContentDialog_Simple")]
	public sealed partial class ContentDialog_Simple : UserControl
	{
		public ContentDialog_Simple()
		{
			this.InitializeComponent();
		}

		private async void OnMyButtonClick(object sender, object args)
		{
			var result = await new ContentDialog_Simple_Dialog().ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick2(object sender, object args)
		{
			var result = await new ContentDialog_Simple_Dialog() { CloseButtonText = "Close" }.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick3(object sender, object args)
		{
			var result = await new ContentDialog_Simple_Dialog() { IsPrimaryButtonEnabled = false }.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick4(object sender, object args)
		{
			var result = await new ContentDialog_Simple_Dialog() { PrimaryButtonCommand = new DelegateCommand(() => commandResult.Text = "primaryCommand") }.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick5(object sender, object args)
		{
			var result = await new ContentDialog_Simple_Dialog() { DefaultButton = ContentDialogButton.Primary }.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		ContentDialog_Simple_Dialog _dialog6;

		private async void OnMyButtonClick6(object sender, object args)
		{
			if (_dialog6 == null)
			{
				_dialog6 = new ContentDialog_Simple_Dialog { };
			}

			var result = await _dialog6.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private void ContentDialog_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs args)
		{

		}

		private void ContentDialog_SecondaryButtonClick(object sender, ContentDialogButtonClickEventArgs args)
		{

		}
	}
}
