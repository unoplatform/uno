using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[SampleControlInfo("TextBox", "PasswordBox_PasswordChar", IsManualTest = true, Description = "PasswordChar is supported on Skia targets. Setting its value should change the password char accordingly.")]
	public sealed partial class PasswordBox_PasswordChar : UserControl
	{
		public PasswordBox_PasswordChar()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			UpdateValueDisplays();
		}

		private void SetDefaultChar_Click(object sender, RoutedEventArgs e)
		{
			DynamicPasswordBox.PasswordChar = "●";
			UpdateValueDisplays();
		}

		private void SetAsterisk_Click(object sender, RoutedEventArgs e)
		{
			DynamicPasswordBox.PasswordChar = "*";
			UpdateValueDisplays();
		}

		private void SetQuestion_Click(object sender, RoutedEventArgs e)
		{
			DynamicPasswordBox.PasswordChar = "?";
			UpdateValueDisplays();
		}

		private void SetDollar_Click(object sender, RoutedEventArgs e)
		{
			DynamicPasswordBox.PasswordChar = "$";
			UpdateValueDisplays();
		}

		private void UpdateValueDisplays()
		{
			DefaultValueText.Text = $"Default: '{DefaultPasswordBox.PasswordChar}'";
			CustomValueText.Text = $"Custom: '{CustomPasswordBox.PasswordChar}'";
			QuestionValueText.Text = $"Question: '{QuestionPasswordBox.PasswordChar}'";
			DollarValueText.Text = $"Dollar: '{DollarPasswordBox.PasswordChar}'";
			DynamicValueText.Text = $"Dynamic: '{DynamicPasswordBox.PasswordChar}'";
		}
	}
}
