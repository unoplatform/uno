using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using UIKit;

namespace Uno.UI.Extensions
{
	internal static class UITextInputExtensions
	{
		public static UITextRange GetTextRange(this IUITextInput textInput, int start, int end)
		{
			if (textInput?.BeginningOfDocument == null)
			{
				return null;
			}

			var from = textInput.GetPosition(textInput.BeginningOfDocument, start);
			var to = textInput.GetPosition(textInput.BeginningOfDocument, end);

			if (from == null || to == null)
			{
				return null;
			}

			return textInput.GetTextRange(from, to);
		}
	}
}
