using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MuxTextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Uno.UI.Xaml.Controls.Extensions;

internal interface IOverlayTextBoxViewExtension
{
	bool IsOverlayLayerInitialized(XamlRoot xamlRoot);

	void StartEntry(bool suppressSoftwareKeyboard = false);

	void EndEntry();

	void UpdateNativeView();

	void InvalidateLayout();

	void UpdateSize();

	void UpdatePosition();

	void NotifyImePositionChanged();

	void SetText(string text);

	void ReplaceText(int start, int length, string replacement);

	void SetPasswordRevealState(PasswordRevealState passwordRevealState);

	void Select(int start, int length);

	int GetSelectionStart();

	int GetSelectionLength();

	void UpdateProperties();

	int GetSelectionStartBeforeKeyDown();

	int GetSelectionLengthBeforeKeyDown();
}
