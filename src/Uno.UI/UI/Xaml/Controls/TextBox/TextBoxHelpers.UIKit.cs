using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal class TextBoxHelpers
{
	public static void SetSpellCheckEnabled()
	{
		_textBoxView.SpellCheckingType = newValue
					? UITextSpellCheckingType.Yes
					: UITextSpellCheckingType.No;

		_textBoxView.AutocorrectionType = newValue
			? UITextAutocorrectionType.Yes
			: UITextAutocorrectionType.No;

		if (newValue)
		{
			_textBoxView.AutocapitalizationType = UITextAutocapitalizationType.Sentences;
		}
	}
}
