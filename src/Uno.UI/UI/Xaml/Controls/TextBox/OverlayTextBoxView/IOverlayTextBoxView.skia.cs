#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls.Extensions;

internal interface ITextBoxView
{
	bool IsCompatible(TextBox textBox);

	void SetFocus(bool isFocused);

	void AddToTextInputLayer(XamlRoot xamlRoot);

	void RemoveFromTextInputLayer();

	bool IsDisplayed { get; }

	void SetPosition(double x, double y);

	void SetSize(double width, double height);

	(int start, int length) Selection { get; set; }

	void UpdateProperties(TextBox textBox);

	IDisposable ObserveTextChanges(EventHandler onChanged);

	string Text { get; set; }
}
