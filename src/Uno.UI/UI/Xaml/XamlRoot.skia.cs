#nullable enable

using Uno.UI.Xaml.Core;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml;

partial class XamlRoot
{
	private FocusManager? _focusManager;

	internal void InvalidateOverlays()
	{
		_focusManager ??= VisualTree.GetFocusManagerForElement(Content);
		_focusManager?.FocusRectManager?.RedrawFocusVisual();
		if (_focusManager?.FocusedElement is TextBox textBox)
		{
			textBox.TextBoxView?.Extension?.InvalidateLayout();
		}
	}
}
