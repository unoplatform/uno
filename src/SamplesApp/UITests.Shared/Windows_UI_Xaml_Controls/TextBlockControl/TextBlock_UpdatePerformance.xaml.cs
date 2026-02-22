using Uno.UI.Samples.Controls;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", Name = "TextBlock_UpdatePerformance")]
	public sealed partial class TextBlock_UpdatePerformance : UserControl
	{
		public TextBlock_UpdatePerformance()
		{
			this.InitializeComponent();

			text1Button.Click += async delegate
			{
				for (var i = 0; i < 1000; i++)
				{
					text1.Text = i.ToString();
					await Task.Yield();
				}
			};
		}
	}
}
