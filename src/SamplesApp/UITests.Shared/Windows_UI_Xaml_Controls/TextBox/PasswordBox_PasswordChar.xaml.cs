using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[SampleControlInfo("TextBox", "PasswordBox_PasswordChar")]
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

		private void SetSquare_Click(object sender, RoutedEventArgs e)
		{
			DynamicPasswordBox.PasswordChar = "■";
			UpdateValueDisplays();
		}

		private void SetKorean_Click(object sender, RoutedEventArgs e)
		{
			DynamicPasswordBox.PasswordChar = "한";
			UpdateValueDisplays();
		}

		private void UpdateValueDisplays()
		{
			DefaultValueText.Text = $"Default: '{DefaultPasswordBox.PasswordChar}'";
			CustomValueText.Text = $"Custom: '{CustomPasswordBox.PasswordChar}'";
			SquareValueText.Text = $"Square: '{SquarePasswordBox.PasswordChar}'";
			KoreanValueText.Text = $"Korean: '{KoreanPasswordBox.PasswordChar}'";
			DynamicValueText.Text = $"Dynamic: '{DynamicPasswordBox.PasswordChar}'";
		}
	}
}