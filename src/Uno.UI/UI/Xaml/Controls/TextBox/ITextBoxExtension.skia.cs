using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Xaml.Controls.Extensions
{
	internal interface ITextBoxViewExtension
	{
		bool IsNativeOverlayLayerInitialized { get; }

		void StartEntry();

		void EndEntry();

		void UpdateNativeView();

		void InvalidateLayout();

		void UpdateSize();

		void UpdatePosition();

		void SetText(string text);

		void SetIsPassword(bool isPassword);

		void Select(int start, int length);

		int GetSelectionStart();

		int GetSelectionLength();

		void UpdateProperties();
	}
}
