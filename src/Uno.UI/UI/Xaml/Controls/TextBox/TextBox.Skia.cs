using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private TextBoxView _textBoxView;

		partial void OnForegroundColorChangedPartial(Brush newValue) => _textBoxView?.OnForegroundChanged(newValue);

		private void UpdateTextBoxView()
		{
			_textBoxView ??= new TextBoxView(this);
			if (_contentElement?.Content != _textBoxView.DisplayBlock)
			{
				_contentElement.Content = _textBoxView.DisplayBlock;
			}
		}

		partial void OnFocusStateChangedPartial(FocusState focusState) => _textBoxView?.OnFocusStateChanged(focusState);
	}
}
