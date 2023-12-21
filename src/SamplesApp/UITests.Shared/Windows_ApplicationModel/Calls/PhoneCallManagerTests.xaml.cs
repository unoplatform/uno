using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.Calls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_ApplicationModel.Calls
{
	[Sample("Windows.ApplicationModel.Calls", Name = "PhoneCallManager")]
	public sealed partial class PhoneCallManagerTests : UserControl
	{
		public PhoneCallManagerTests()
		{
			this.InitializeComponent();

			try
			{
				PhoneCallManager.CallStateChanged += PhoneCallManager_CallStateChanged;
				UpdateCallState();
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.ToString();
			}
		}

		private void PhoneCallManager_CallStateChanged(object sender, object e) => UpdateCallState();

		private async void UpdateCallState()
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				IsCallActiveCheckBox.IsChecked = PhoneCallManager.IsCallActive;
				IsCallIncomingCheckBox.IsChecked = PhoneCallManager.IsCallIncoming;
			});
		}

		private void ShowPhoneSettings_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PhoneCallManager.ShowPhoneCallSettingsUI();
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.ToString();
			}
		}

		private void Dial_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				PhoneCallManager.ShowPhoneCallUI(PhoneNumberTextBox.Text, "Jon Doe");
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.ToString();
			}
		}
	}
}
