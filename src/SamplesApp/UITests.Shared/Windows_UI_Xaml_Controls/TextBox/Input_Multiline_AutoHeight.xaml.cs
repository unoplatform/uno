using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", Name = "Input_Multiline_AutoHeight", ViewModelType = typeof(TextBoxViewModel))]
	public sealed partial class Input_Multiline_AutoHeight : UserControl
	{
		public Input_Multiline_AutoHeight()
		{
			InitializeComponent();
		}

		private void BtnSingle_OnClick(object sender, RoutedEventArgs e)
		{
			(DataContext as TextBoxViewModel).MyInput = "Single Line";
		}

		private void BtnDouble_OnClick(object sender, RoutedEventArgs e)
		{
			(DataContext as TextBoxViewModel).MyInput = "Double\nLine";
		}

	}
}
