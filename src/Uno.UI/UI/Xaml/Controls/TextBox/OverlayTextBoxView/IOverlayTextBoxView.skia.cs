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

	/// <summary>
	/// Returns a value indicating whether this TextBoxView is compatible with the given TextBox state.
	/// </summary>
	/// <param name="textBox">TextBox.</param>
	/// <returns>True if compatible.</returns>
	bool IsCompatible(TextBox textBox);

	void SetFocus();

	void SetPasswordRevealState(PasswordRevealState passwordRevealState);

	void AddToTextInputLayer(XamlRoot xamlRoot);

	void RemoveFromTextInputLayer();

	void SetPosition(double x, double y);

	void SetSize(double width, double height);

	void UpdateProperties(TextBox textBox);

	IDisposable ObserveTextChanges(EventHandler onChanged);
}
