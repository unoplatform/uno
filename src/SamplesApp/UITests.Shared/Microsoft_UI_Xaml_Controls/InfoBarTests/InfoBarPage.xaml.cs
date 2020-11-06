using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml.Automation;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.InfoBarTests
{
	[Sample("InfoBar", "WinUI")]
	public sealed partial class InfoBarPage : Page
	{
		public InfoBarPage()
		{
			this.InitializeComponent();
		}
#if HAS_UNO
		private void SeverityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string severityName = e.AddedItems[0].ToString();

			switch (severityName)
			{
				case "Error":
					TestInfoBar.Severity = InfoBarSeverity.Error;
					break;

				case "Warning":
					TestInfoBar.Severity = InfoBarSeverity.Warning;
					break;

				case "Success":
					TestInfoBar.Severity = InfoBarSeverity.Success;
					break;

				case "Informational":
				default:
					TestInfoBar.Severity = InfoBarSeverity.Informational;
					break;
			}
		}

		private void ActionButtonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TestInfoBar == null) return;

			if (ActionButtonComboBox.SelectedIndex == 0)
			{
				TestInfoBar.ActionButton = null;
			}
			else if (ActionButtonComboBox.SelectedIndex == 1)
			{
				var button = new Button();
				button.Content = "Action";
				TestInfoBar.ActionButton = button;
			}
			else if (ActionButtonComboBox.SelectedIndex == 2)
			{
				var link = new HyperlinkButton();
				link.NavigateUri = new Uri("http://www.microsoft.com/");
				link.Content = "Informational link";
				TestInfoBar.ActionButton = link;
			}
		}

		private void IconComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TestInfoBar == null) return;

			switch (e.AddedItems[0].ToString())
			{
				case "Custom Icon":
					Microsoft.UI.Xaml.Controls.SymbolIconSource symbolIcon = new Microsoft.UI.Xaml.Controls.SymbolIconSource();
					symbolIcon.Symbol = Symbol.Pin;
					TestInfoBar.IconSource = (Microsoft.UI.Xaml.Controls.IconSource)symbolIcon;
					break;

				case "Default Icon":
				default:
					TestInfoBar.IconSource = null;
					break;
			}
		}

		public void OnCloseButtonClick(object sender, object args)
		{
			EventListBox.Items.Add("CloseButtonClick");
		}

		public void OnClosing(object sender, InfoBarClosingEventArgs args)
		{
			EventListBox.Items.Add("Closing: " + args.Reason);

			if (CancelCheckBox.IsChecked.Value)
			{
				args.Cancel = true;
			}
		}

		public void OnClosed(object sender, InfoBarClosedEventArgs args)
		{
			EventListBox.Items.Add("Closed: " + args.Reason);
		}

		public void ClearButtonClick(object sender, object args)
		{
			EventListBox.Items.Clear();
		}

		public void SetForegroundClick(object sender, object args)
		{
			TestInfoBar.Foreground = new SolidColorBrush(Colors.Red);
		}

		public void HasCustomContentChanged(object sender, object args)
		{
			if (HasCustomContentCheckBox.IsChecked.Value)
			{
				var content = new CheckBox();
				content.Content = "Custom Content";
				content.Margin = new Thickness(0, 0, 0, 6);
				AutomationProperties.SetName(content, "CustomContentCheckBox");
				TestInfoBar.Content = content;
			}
			else
			{
				TestInfoBar.Content = null;
			}
		}

		public void CloseStyleChanged(object sender, object args)
		{
			if (CloseButtonStyleCheckBox.IsChecked.Value)
			{
				TestInfoBar.CloseButtonStyle = this.Resources["CustomCloseButtonStyle"] as Style;
			}
			else
			{
				TestInfoBar.CloseButtonStyle = Application.Current.Resources["InfoBarCloseButtonStyle"] as Style;
			}
		}

		public void CustomBackgroundChanged(object sender, object args)
		{
			if (CustomBackgroundCheckBox.IsChecked.Value)
			{
				TestInfoBar.Background = new SolidColorBrush(Colors.Purple);
			}
			else
			{
				TestInfoBar.Background = new SolidColorBrush(Colors.Transparent);
			}
		}
#endif
	}
}
