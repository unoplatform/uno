#nullable enable

using System;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls.Extensions;

internal interface ITextBoxView
{
	bool IsCompatible(TextBox textBox);

	void SetFocus(bool isFocused);

	void AddToTextInputLayer(WpfCanvas layer);

	void RemoveFromTextInputLayer();

	bool IsDisplayed { get; }

	void SetPosition(double x, double y);

	void SetSize(double width, double height);

	(int start, int end) GetSelectionBounds();

	void SetSelectionBounds(int start, int end);

	void UpdateProperties(TextBox textBox);

	IDisposable ObserveTextChanges(EventHandler onChanged);

	string Text { get; set; }
}
