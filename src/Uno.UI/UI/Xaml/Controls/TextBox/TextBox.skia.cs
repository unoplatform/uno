using Uno.UI;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Xaml.Media;

#nullable enable

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : IBlock
	{
		private ITextBoxView? _textBoxView;
		private InlineCollection? _inlines;

		internal ITextBoxView? TextBoxView => _textBoxView;

		internal ContentControl ContentElement => _contentElement;

		partial void OnForegroundColorChangedPartial(Brush newValue) => TextBoxView?.OnForegroundChanged(newValue);

		partial void OnMaxLengthChangedPartial(DependencyPropertyChangedEventArgs e) => TextBoxView?.UpdateMaxLength();

		private void UpdateTextBoxView()
		{
			_textBoxView ??= FeatureConfiguration.TextBox.UseSkiaOnlyImplementation
				? new NativeTextBoxView(this)
				: new SkiaTextBoxView(this);

			if (ContentElement != null && ContentElement.Content != _textBoxView.Content)
			{
				ContentElement.Content = _textBoxView.Content;
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

		protected void SetIsPassword(bool isPassword) => TextBoxView?.SetIsPassword(isPassword);

		// Remaining IBlock properties that TextBox does not support:

		double IBlock.LineHeight => 0;

		LineStackingStrategy IBlock.LineStackingStrategy => LineStackingStrategy.MaxHeight;

		TextTrimming IBlock.TextTrimming => TextTrimming.None;

		int IBlock.MaxLines => 0;
	}
}
