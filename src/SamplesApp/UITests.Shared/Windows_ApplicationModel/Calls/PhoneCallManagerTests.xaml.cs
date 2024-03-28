using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.Calls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Private.Infrastructure;

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
			await UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
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
