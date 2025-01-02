// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InfoBarPage.xaml.cs, commit b424312
#pragma warning disable CS0105 // duplicate namespace because of WinUI source conversion

using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

using IconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.IconSource;
using SymbolIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SymbolIconSource;

using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("Info", "MUX")]
	public sealed partial class InfoBarPage : TestPage
	{
		private ButtonBase _actionButton = null;
		private DispatcherTimer _timer = new DispatcherTimer();
		private int _timerAction = 0;

		public InfoBarPage()
		{
			this.InitializeComponent();

			_timer.Interval = new TimeSpan(0, 0, 8 /*sec*/);
			_timer.Tick += Timer_Tick;
		}

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

			DiscardActionButton();

			if (ActionButtonComboBox.SelectedIndex == 0)
			{
				TestInfoBar.ActionButton = null;
			}
			else if (ActionButtonComboBox.SelectedIndex == 1)
			{
				var button = new Button();
				button.Content = "Action";
				button.Click += ActionButton_Click;
				_actionButton = TestInfoBar.ActionButton = button;
			}
			else if (ActionButtonComboBox.SelectedIndex == 2)
			{
				var link = new HyperlinkButton();
				link.NavigateUri = new Uri("http://www.microsoft.com/");
				link.Content = "Informational link";
				link.Click += ActionButton_Click;
				_actionButton = TestInfoBar.ActionButton = link;
			}
			else if (ActionButtonComboBox.SelectedIndex == 3)
			{
				var button = new Button();
				button.Content = "Action";
				button.HorizontalAlignment = HorizontalAlignment.Right;
				button.Click += ActionButton_Click;
				_actionButton = TestInfoBar.ActionButton = button;
			}
			else if (ActionButtonComboBox.SelectedIndex == 4)
			{
				var link = new HyperlinkButton();
				link.NavigateUri = new Uri("http://www.microsoft.com/");
				link.Content = "Informational link";
				link.HorizontalAlignment = HorizontalAlignment.Right;
				link.Click += ActionButton_Click;
				_actionButton = TestInfoBar.ActionButton = link;
			}

			if (_actionButton != null)
			{
				// Workaround for GitHub Issue #4531.
				_actionButton.TabIndex = 1;
			}
		}

		private void ActionButton_Click(object sender, RoutedEventArgs e)
		{
			EventListBox.Items.Add("ActionButtonClick");

			if (RemoveActionButtonOnInvokeCheckBox.IsChecked.Value)
			{
				ResetActionButtonProperty();
			}
		}

		private void CollapseActionButtonProperty()
		{
			if (_actionButton != null)
			{
				_actionButton.Visibility = Visibility.Collapsed;
			}
		}

		private void DisableActionButtonProperty()
		{
			if (_actionButton != null)
			{
				_actionButton.IsEnabled = false;
			}
		}

		private void ResetActionButtonProperty()
		{
			ActionButtonComboBox.SelectedIndex = 0;
		}

		private void DiscardActionButton()
		{
			if (_actionButton != null)
			{
				_actionButton.Click -= ActionButton_Click;
				_actionButton = null;
			}
		}

		private void Timer_Tick(object sender, object e)
		{
			_timer.Stop();

			switch (_timerAction)
			{
				case 0:
					CollapseActionButtonProperty();
					break;
				case 1:
					DisableActionButtonProperty();
					break;
				case 2:
					ResetActionButtonProperty();
					break;
				case 3:
					InfoBarParent.Children.Remove(TestInfoBar);
					AddInfoBarButton.IsEnabled = true;
					RemoveInfoBarAsynchronouslyButton.IsEnabled = false;
					break;
			}

			switch (_timerAction)
			{
				case 0:
				case 1:
				case 2:
					CollpaseActionButtonAsynchronouslyButton.IsEnabled = true;
					DisableActionButtonAsynchronouslyButton.IsEnabled = true;
					ResetActionButtonAsynchronouslyButton.IsEnabled = true;
					RemoveInfoBarAsynchronouslyButton.IsEnabled = true;
					break;
				case 3:
					AddInfoBarButton.IsEnabled = true;
					break;
			}
		}

		private void IconComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TestInfoBar == null) return;

			switch (e.AddedItems[0].ToString())
			{
				case "Custom Icon":
					SymbolIconSource symbolIcon = new SymbolIconSource();
					symbolIcon.Symbol = Symbol.Pin;
					TestInfoBar.IconSource = (IconSource)symbolIcon;
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

		private void CollapseActionButtonAsynchronouslyClick(object sender, object args)
		{
			StartAsynchronousAction(0);
		}

		private void DisableActionButtonAsynchronouslyClick(object sender, object args)
		{
			StartAsynchronousAction(1);
		}

		private void ResetActionButtonAsynchronouslyClick(object sender, object args)
		{
			StartAsynchronousAction(2);
		}

		private void AddInfoBarClick(object sender, object args)
		{
			InfoBarParent.Children.Insert(0, TestInfoBar);
			AddInfoBarButton.IsEnabled = false;
			CollpaseActionButtonAsynchronouslyButton.IsEnabled = true;
			DisableActionButtonAsynchronouslyButton.IsEnabled = true;
			ResetActionButtonAsynchronouslyButton.IsEnabled = true;
			RemoveInfoBarAsynchronouslyButton.IsEnabled = true;
		}

		private void RemoveInfoBarAsynchronouslyClick(object sender, object args)
		{
			StartAsynchronousAction(3);
		}

		private void StartAsynchronousAction(int timerAction)
		{
			_timer.Start();
			_timerAction = timerAction;

			CollpaseActionButtonAsynchronouslyButton.IsEnabled = false;
			DisableActionButtonAsynchronouslyButton.IsEnabled = false;
			ResetActionButtonAsynchronouslyButton.IsEnabled = false;
			AddInfoBarButton.IsEnabled = false;
			RemoveInfoBarAsynchronouslyButton.IsEnabled = false;
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
	}
}
