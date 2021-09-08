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

		partial void SelectPartial(int start, int length)
		{
			_textBoxView?.Select(start, length);
		}

		partial void SelectAllPartial() => Select(0, Text.Length);

		public int SelectionStart
		{
			get => _textBoxView?.GetSelectionStart() ?? 0;
			set => Select(start: value, length: SelectionLength);
		}

		public int SelectionLength
		{
			get => _textBoxView?.GetSelectionLength() ?? 0;
			set => Select(SelectionStart, value);
		}


		protected void SetIsPassword(bool isPassword) => _textBoxView?.SetIsPassword(isPassword);
	}
}
