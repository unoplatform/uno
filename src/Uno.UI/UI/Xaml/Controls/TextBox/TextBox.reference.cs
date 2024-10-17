using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private TextBoxView _textBoxView;

		private void UpdateTextBoxView() { }

		public int SelectionStart { get; set; }

		public int SelectionLength { get; set; }

		protected override void OnPointerMoved(PointerRoutedEventArgs e)
		{
			base.OnPointerMoved(e);
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			base.OnDoubleTapped(e);
		}

		protected override void OnRightTapped(RightTappedRoutedEventArgs e)
		{
			base.OnRightTapped(e);
		}
	}
}
