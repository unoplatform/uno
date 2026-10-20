#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DirectUI;

partial class TextBoxPlaceholderTextHelper
{
#if HAS_UNO
	private static bool IsTextControlEmpty(UIElement textControl) => textControl switch
	{
		RichEditBox richEditBox => richEditBox.Document.GetRange(0, 2).StoryLength <= 1,
		ITextBoxHost textBox => textBox.Core.IsEmpty,
		_ => false,
	};
#endif
}
