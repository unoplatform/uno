using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.PasswordBoxTests
{
	[Sample(
		"TextBox",
		Name = "PasswordBox_AutoFill_Login",
		IsManualTest = true,
		Description = "Login form for the platform password manager. Picking a saved login from the keyboard should fill both the e-mail and the password, which the readouts show. Confirm removes the form, as a sign-in that navigates away would.")]
	public sealed partial class PasswordBox_AutoFill : Page
	{
		public PasswordBox_AutoFill()
		{
			this.InitializeComponent();
		}

		private void OnEmailChanged(object sender, TextChangedEventArgs e)
			=> EmailReadout.Text = $"E-mail: {(string.IsNullOrEmpty(EmailBox.Text) ? "(empty)" : $"\"{EmailBox.Text}\"")}";

		private void OnPasswordChanged(object sender, RoutedEventArgs e)
			=> PasswordReadout.Text = $"Password: {(string.IsNullOrEmpty(PasswordInput.Password) ? "(empty)" : $"{PasswordInput.Password.Length} chars")}";

		private void OnConfirm(object sender, RoutedEventArgs e)
		{
			FormHost.Children.Remove(LoginForm);
			ShowFormButton.Visibility = Visibility.Visible;
		}

		private void OnShowForm(object sender, RoutedEventArgs e)
		{
			FormHost.Children.Insert(0, LoginForm);
			ShowFormButton.Visibility = Visibility.Collapsed;
		}
	}
}
