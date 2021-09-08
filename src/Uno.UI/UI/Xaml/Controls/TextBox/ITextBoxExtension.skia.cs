using Windows.Foundation;

namespace Uno.UI.Xaml.Controls.Extensions
{
	internal interface ITextBoxViewExtension
    {
		void StartEntry();

		void EndEntry();

		void UpdateNativeView();

		void UpdateSize();

		void UpdatePosition();

		void SetTextNative(string text);

		void SetIsPassword(bool isPassword);

		void Select(int start, int length);

		int GetSelectionStart();

		int GetSelectionLength();
	}
}
