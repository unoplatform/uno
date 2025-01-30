using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI;
using Uno.UI.Common;
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

using ICommand = System.Windows.Input.ICommand;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests
{
	[SampleControlInfo("Dialogs", "ContentDialog_Simple")]
	public sealed partial class ContentDialog_Simple : UserControl
	{
		public ContentDialog_Simple()
		{
			this.InitializeComponent();

#if __ANDROID__
			var window = (ContextHelper.Current as global::Android.App.Activity).Window;
			var rect = new global::Android.Graphics.Rect();
			window.DecorView.GetWindowVisibleDisplayFrame(rect);
			int height = rect.Top;
			int contentViewTop =
				window.FindViewById(global::Android.Views.Window.IdAndroidContent).Top;
			int titleBarHeight = contentViewTop - height;
			statusBarHeight.Text = titleBarHeight.ToString();
#endif
		}

		private async void OnMyButtonClick(object sender, object args)
		{
			var dialog = new ContentDialog_Simple_Dialog();
			dialog.XamlRoot = this.XamlRoot;
			var result = await dialog.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick2(object sender, object args)
		{
			var dialog = new ContentDialog_Simple_Dialog() { CloseButtonText = "Close" };
			dialog.XamlRoot = this.XamlRoot;
			var result = await dialog.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick3(object sender, object args)
		{
			var dialog = new ContentDialog_Simple_Dialog() { IsPrimaryButtonEnabled = false };
			dialog.XamlRoot = this.XamlRoot;
			var result = await dialog.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick4(object sender, object args)
		{
			var dialog = new ContentDialog_Simple_Dialog() { PrimaryButtonCommand = new DelegateCommand(() => commandResult.Text = "primaryCommand") };
			dialog.XamlRoot = this.XamlRoot;
			var result = await dialog.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		private async void OnMyButtonClick5(object sender, object args)
		{
			var dialog = new ContentDialog_Simple_Dialog() { DefaultButton = ContentDialogButton.Primary };
			dialog.XamlRoot = this.XamlRoot;
			var result = await dialog.ShowAsync();
			dialogResult.Text = result.ToString();
		}

		ContentDialog_Simple_Dialog _dialog6;

		private async void OnMyButtonClick6(object sender, object args)
		{
			if (_dialog6 == null)
			{
				_dialog6 = new ContentDialog_Simple_Dialog { };
			}

			_dialog6.XamlRoot = this.XamlRoot;
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
