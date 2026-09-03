#nullable enable

using DirectUI;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar : IBackButtonListener
{
	bool IBackButtonListener.OnBackButtonPressed()
	{
		OnBackButtonPressedImpl(out var handled);
		return handled;
	}
}
