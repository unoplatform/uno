using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[SampleControlInfo("TextBlockControl", nameof(TextBlock_TextWrapping_PR1954_EdgeCase), description: "[Droid]Repro sample for PR1954. When the text is limited to 1 line, due to enough height available for 2 lines, it should take the size of 1 line only. You may need to adjust text lenght and/or container height depending on the device. Expected behavior: text should always be in the center of pink area")]
	public sealed partial class TextBlock_TextWrapping_PR1954_EdgeCase : UserControl
	{
		public TextBlock_TextWrapping_PR1954_EdgeCase()
		{
			this.InitializeComponent();
		}

		private void AdjustTextLength(object sender, RoutedEventArgs e)
		{
			if (GetButtonDirection(sender) is bool direction)
			{
				var text = SampleText.Text;
				if (text.Length > 0 || direction)
				{
					SampleText.Text = direction
						? text + "Xy"[text.Length % 2]
						: text.Substring(0, text.Length - 1);
				}
			}
		}

		private void AdjustContainerHeight(object sender, RoutedEventArgs e)
		{
			if (GetButtonDirection(sender) is bool direction)
			{
				if (SampleContainer.Height + (direction ? 5 : -5) is var finalHeight && finalHeight > 0)
				{
					SampleContainer.Height = finalHeight;
				}
			}
		}

		private bool? GetButtonDirection(object sender)
		{
			switch ((sender as Windows.UI.Xaml.Controls.Button)?.Content)
			{
				case "+": return true;
				case "-": return false;

				default: return default;
			}
		}
	}
}
