using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using System;
using System.Linq;

#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", IsManualTest = true, IgnoreInSnapshotTests = true, Name = "TextBox_InputReturnType", Description = "Select a input return type and tap the input to see the Enter button display change accordingly.")]
	public sealed partial class TextBox_InputReturnType : UserControl
	{
		public TextBox_InputReturnType()
		{
			this.InitializeComponent();
		}

#if HAS_UNO
		public InputReturnType[] InputReturnTypes { get; } = Enum.GetValues<InputReturnType>().ToArray();
#endif

		private void SetHintClick(object sender, object e)
		{
#if HAS_UNO
			var selectedValue = (InputReturnType)InputReturnTypesComboBox.SelectedItem;
			TextBoxExtensions.SetInputReturnType(TestTextBox, selectedValue);
#endif
		}
	}
}
