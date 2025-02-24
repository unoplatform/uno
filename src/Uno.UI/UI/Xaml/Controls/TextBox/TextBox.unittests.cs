using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private TextBoxView _textBoxView;

		private void UpdateTextBoxView() { }

		public int SelectionStart { get; set; }

		public int SelectionLength { get; set; }
	}
}
