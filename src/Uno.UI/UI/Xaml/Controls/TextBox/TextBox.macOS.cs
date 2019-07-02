using System;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : Control
	{
		private TextBoxView _textBoxView;
		
		protected override bool RequestFocus(FocusState state) => FocusTextView();
		partial void OnTextClearedPartial() => FocusTextView();
		protected virtual bool FocusTextView() => throw new NotImplementedException();

		partial void OnAcceptsReturnChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			// TODO macOS
			_textBoxView?.ToString();
		}


		private void UpdateTextBoxView()
		{
			throw new NotImplementedException();
		}

		protected void SetIsPassword(bool isPassword)
		{

		}

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{

		}

		partial void UpdateFontPartial()
		{

		}

		partial void OnTextWrappingChangedPartial(DependencyPropertyChangedEventArgs e)
		{

		}

		partial void OnTextAlignmentChangedPartial(DependencyPropertyChangedEventArgs e)
		{

		}

		partial void OnIsSpellCheckEnabledChangedPartial(DependencyPropertyChangedEventArgs e)
		{

		}
	}
}
