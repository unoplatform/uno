using System.Collections.Generic;
using System.Reflection;
using Uno.UI.Samples.Controls;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", Name = "PasswordBox_AutoFill", IsManualTest = true)]
	public sealed partial class PasswordBox_AutoFill : UserControl
	{
		public PasswordBox_AutoFill()
		{
			this.InitializeComponent();
		}

		// Live readouts show the values the app actually received (e.g. from autofill), not just what is drawn.
		private void OnEmailChanged(object sender, TextChangedEventArgs e)
			=> EmailReadout.Text = $"Email: {Describe(EmailBox.Text)}";

		private void OnPasswordChanged(object sender, RoutedEventArgs e)
			=> PasswordReadout.Text = $"Password: {DescribePassword(PasswordBox.Password)}";

		private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
			=> ConfirmPasswordReadout.Text = $"Confirm password: {DescribePassword(ConfirmPasswordBox.Password)}";

		private static string Describe(string value)
			=> string.IsNullOrEmpty(value) ? "(empty)" : $"\"{value}\"";

		private static string DescribePassword(string value)
			=> string.IsNullOrEmpty(value) ? "(empty)" : $"{value.Length} chars";
	}
}
