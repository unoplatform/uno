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
    }
}
