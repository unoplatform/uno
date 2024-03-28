using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class TextBoxBeforeTextChangingEventArgs
	{
		internal TextBoxBeforeTextChangingEventArgs(string newText)
		{
			NewText = newText;
		}

		public bool Cancel { get; set; }

		public string NewText { get; }
	}
}
