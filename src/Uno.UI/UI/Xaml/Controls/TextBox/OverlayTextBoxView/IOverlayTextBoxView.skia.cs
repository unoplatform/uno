#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls.Extensions;

internal interface IOverlayTextBoxView
{
	bool IsDisplayed { get; }

	string Text { get; set; }

	(int start, int length) Selection { get; set; }

	bool IsCompatible(TextBox textBox);

	void SetFocus(bool isFocused);

	void SetPasswordRevealState(PasswordRevealState passwordRevealState);

	void AddToTextInputLayer(XamlRoot xamlRoot);

	void RemoveFromTextInputLayer();

	void SetPosition(double x, double y);

	void SetSize(double width, double height);

	void UpdateProperties(TextBox textBox);

	IDisposable ObserveTextChanges(EventHandler onChanged);
}
