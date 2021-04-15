using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private TextBoxView _textBoxView;
		
		internal ContentControl ContentElement => _contentElement;

		partial void OnForegroundColorChangedPartial(Brush newValue) => _textBoxView?.OnForegroundChanged(newValue);

		partial void OnMaxLengthChangedPartial(DependencyPropertyChangedEventArgs e) => _textBoxView?.UpdateMaxLength();

		private void UpdateTextBoxView()
		{
			_textBoxView ??= new TextBoxView(this);
			if (ContentElement != null && ContentElement.Content != _textBoxView.DisplayBlock)
			{
				ContentElement.Content = _textBoxView.DisplayBlock;
			}
		}

		partial void OnFocusStateChangedPartial(FocusState focusState) => _textBoxView?.OnFocusStateChanged(focusState);
	}
}
