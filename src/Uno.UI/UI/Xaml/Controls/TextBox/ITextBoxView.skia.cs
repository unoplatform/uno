using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal interface ITextBoxView
	{
		TextBox TextBox { get; }

		UIElement Content { get; }

		int GetSelectionLength();

		int GetSelectionStart();

		void OnFocusStateChanged(FocusState focusState);

		void OnForegroundChanged(Brush brush);

		void Select(int start, int length);

		void SetIsPassword(bool isPassword);

		void SetText(string text);

		void UpdateMaxLength();

		void InvalidateLayout();
	}
}
