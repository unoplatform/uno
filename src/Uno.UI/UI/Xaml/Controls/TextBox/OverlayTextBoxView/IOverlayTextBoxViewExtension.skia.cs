using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls.Extensions;

internal interface IOverlayTextBoxViewExtension
{
	bool IsOverlayLayerInitialized(XamlRoot xamlRoot);

	void StartEntry();

	void EndEntry();

	void UpdateNativeView();

	void InvalidateLayout();

	void UpdateSize();

	void UpdatePosition();

	void SetText(string text);

	void SetPasswordRevealState(PasswordRevealState passwordRevealState);

	void Select(int start, int length);

	int GetSelectionStart();

	int GetSelectionLength();

	int GetSelectionStartBeforeKeyDown();

	int GetSelectionLengthBeforeKeyDown();

	void UpdateProperties();
}
