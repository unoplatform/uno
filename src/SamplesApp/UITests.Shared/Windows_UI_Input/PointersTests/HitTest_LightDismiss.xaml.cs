#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Input.PointersTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Pointers", "Popup")]
	public sealed partial class HitTest_LightDismiss : Page
	{
		private Popup _popup;
		public HitTest_LightDismiss()
		{
			this.InitializeComponent();

			var popupChild = new Border
			{
				Width = 220,
				Height = 140,
				Background = new SolidColorBrush(Colors.DarkGreen),
				Child = new TextBlock
				{
					Text = "X",
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
				}
			};
			_popup = new Popup
			{
				Child = popupChild
			};
			AutomationProperties.SetAutomationId(popupChild, "TargetPopupContent");
			popupChild.PointerPressed += (o, e) => ResultTextBlock.Text = "Popup content pressed";

			void OnFlyoutOpenedOrClosed(object? sender, object? args)
			{
				FlyoutStatusTextBlock.Text = sender is Flyout flyout ? flyout.IsOpen.ToString() : "";
			}
			ButtonFlyout.Opened += OnFlyoutOpenedOrClosed;
			ButtonFlyout.Closed += OnFlyoutOpenedOrClosed;

			void OnPopupOpenedOrClosed(object? sender, object? args)
			{
				PopupStatusTextBlock.Text = sender is Popup popup ? popup.IsOpen.ToString() : "";
			}
			_popup.Opened += OnPopupOpenedOrClosed;
			_popup.Closed += OnPopupOpenedOrClosed;

			TargetComboBox.SelectionChanged += (o, e) =>
			{
				ResultTextBlock.Text = $"Item selected";
			};
		}

		private void LaunchDismissiblePopup(object sender, object args)
		{
			if (!_popup.IsOpen)
			{
				_popup.IsLightDismissEnabled = true;
				_popup.IsOpen = true;
			}
		}

		private void LaunchUndismissiblePopup(object sender, object args)
		{
			if (!_popup.IsOpen)
			{
				_popup.IsLightDismissEnabled = false;
				_popup.IsOpen = true;
			}
		}

		private void ResetResult(object sender, object args)
		{
			ResultTextBlock.Text = "None";
			ButtonFlyout.Hide();
			_popup.IsOpen = false;
			TargetComboBox.SelectedItem = null;
		}

		private void DoAction(object sender, object args)
		{
			//if (!_popup.IsLightDismissEnabled && _popup.IsOpen)
			//{
			//	ResultTextBlock.Text = "Popup closed";
			//	_popup.IsOpen = false;
			//}
			//else
			{
				ResultTextBlock.Text = "Button pressed";

			}
		}

		private void OnFlyoutContentPressed(object sender, object args)
		{
			ResultTextBlock.Text = "Flyout content pressed";
		}
	}
}
