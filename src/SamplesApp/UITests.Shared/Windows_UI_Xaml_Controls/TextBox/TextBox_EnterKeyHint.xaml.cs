using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Uno.UI.Xaml.Controls;
using System;
using System.Linq;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[SampleControlInfo("TextBox", "TextBox_EnterKeyHint", typeof(TextBoxViewModel))]
	public sealed partial class TextBox_EnterKeyHint : UserControl
	{
		public TextBox_EnterKeyHint()
		{
			this.InitializeComponent();
		}

#if __WASM__
		public EnterKeyHint[] EnterKeyHints { get; } = Enum.GetValues<EnterKeyHint>().ToArray();

		private void SetHintClick(object sender, object e)
		{
			var selectedValue = (EnterKeyHint)EnterKeyHintsComboBox.SelectedItem;
			TestTextBox.EnterKeyHint = selectedValue;
		}
#endif
	}
}
