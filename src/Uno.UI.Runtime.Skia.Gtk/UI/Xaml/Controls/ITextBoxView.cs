#nullable enable

using Gtk;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;

internal interface ITextBoxView
{
	void SetFocus(bool isFocused);

	void AddToTextInputLayer(Fixed layer);

	void RemoveFromTextInputLayer();

	void SetSize(double width, double height);
}
